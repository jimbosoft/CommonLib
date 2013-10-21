using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Reflection;

namespace CommonLib.Utilities
{
    /// <summary>
    /// Formats serialized objects using name/value pairs
    /// where pairs are separated using a comma ',' and the name
    /// and value are separated using an equal sign '='.
    /// Serializes objects inherited from ISerializable and with the 
    /// Serializable attibute.
    /// </summary>
    public class TextFormatter : IFormatter
    {
        /// <summary>
        /// Support class for conversion of data from a stream.
        /// Same converter used by BinaryFormatter 
        /// </summary>
        private IFormatterConverter m_Converter
            = new FormatterConverter();

        /// <summary>
        /// IFormatter member variables
        /// </summary>
        private SerializationBinder m_Binder = null;
        private StreamingContext m_Context = new StreamingContext(StreamingContextStates.All);
        private ISurrogateSelector m_Selector = null;

        /// <summary>
        /// Registered types, keyed by the full class name
        /// </summary>
        protected Dictionary<String, Type> m_RegisteredTypes
            = new Dictionary<String, Type>();

        /// <summary>
        /// This is used to build the SerializationInfo values
        /// when reading from a stream in ValueOnly Storage mode.
        /// 
        /// Registered types can have their own specific mapping
        /// of values read from a stream to a specific name.
        /// eg 1st value is called ClassName
        /// 2nd value is called Item1
        /// 3rd value is called Item2....
        /// </summary>
        protected Dictionary<String, List<String>> m_RegisteredNameMaps
            = new Dictionary<String,List<String>>();

        /// <summary>
        /// The storage type indicates how the text is formatted
        /// when written to the stream and the expected format
        /// when reading from the stream.
        /// </summary>
        public enum StorageType 
        {
            NameValue, // Name1=Value,Name2=Value,...
            ValueOnly, // Value,Value,...
            CSV // CSV format follows the standard RFC 4180.
        }

        /// <summary>
        /// Common facility is to include the names of the 
        /// columns as the first line in a delimitered text file.
        /// This is only used for deserializing from a stream.
        /// MUST deserialize the first line for this to be used.
        /// </summary>
        public Boolean UseFirstLineAsColumnNames = false;

        /// <summary>
        /// Enables putting all items output within double quotes
        /// for a CSV file storage type.
        /// </summary>
        public Boolean QuoteAll = false;

        /// <summary>
        /// Used to indicate the current deserialization started 
        /// at the beginning of the stream, ie stream.Position == 0.
        /// The stream must also be readable.
        /// </summary>
        private Boolean m_FirstLine = false; 
        
        /// <summary>
        /// Column names read from the last stream that was deserialized.
        /// Only populated when the first line of the stream is 
        /// deserialized and UseFirstLineAsColumnNames = true.
        /// </summary>
        private List<String> m_FirstLineColumnNames = null;

        /// <summary>
        /// The most recent string read using ReadLine().
        /// </summary>
        private String m_LastLineRead = String.Empty;

        /// <summary>
        /// Read only collection of the column names read from
        /// the last stream that was deserialized.
        /// Only populated when the first line of the stream is 
        /// deserialized and UseFirstLineAsColumnNames = true.
        /// </summary>
        public ReadOnlyCollection<String> FirstLineColumnNames
        {
            get { return m_FirstLineColumnNames.AsReadOnly(); }
        }

        /// <summary>
        /// Parsing delimiters used in the stream to identify 
        /// name/value pairs.
        /// </summary>
        private Char m_NameDelimiter = ',';
        private Char m_ValueDelimiter = '=';
        private Char m_EscapeChar = '\\';
        private StorageType m_StorageType = StorageType.NameValue;

        /// <summary>
        /// Only used for StorageType of ValueOnly
        /// Used to uniquely identify each value
        /// with a name.
        /// </summary>
        private String m_NameIndexFormat = "D10";

        /// <summary>
        /// Indicates the stream text format.
        /// ValueOnly expects values to be stored in a particular order.
        /// NameValue can store items in any order.
        /// </summary>
        public StorageType Storage
        {
            get { return m_StorageType; }
            set { m_StorageType = value; }
        }

        /// <summary>
        /// Delimiter used to separate each item.
        /// When using StorageType.CSV, this is used as the comma
        /// </summary>
        public Char NameDelimiter
        {
            get { return m_NameDelimiter; }
            set { m_NameDelimiter = value; }
        }

        /// <summary>
        /// When Storage is set to NameValue, the Name and
        /// Value are separated using this delimiter.
        /// </summary>
        public Char ValueDelimiter
        {
            get { return m_ValueDelimiter; }
            set { m_ValueDelimiter = value; }
        }

        /// <summary>
        /// Escape character is used to signal the formatter
        /// to treat delimiters and the escape character as 
        /// part of the name or value.
        /// Escape character is not used when using StorageType.CSV.
        /// </summary>
        public Char Escape
        {
            get { return m_EscapeChar; }
            set { m_EscapeChar = value; }
        }

        /// <summary>
        /// The most recent string read using ReadLine().
        /// </summary>
        public String LastLineRead
        {
            get { return m_LastLineRead; }
        }

        /// <summary>
        /// Parse a string into multiple strings using the CSV format
        /// The delimiter is configurable.
        /// Double quotes are used to escape the delimiter.
        /// Double quotes can be included in a string by using 2 double quotes.
        /// 
        /// This is based on the parser at the following web reference:
        /// [ref:http://knab.ws/blog/index.php?/archives/3-CSV-file-parser-and-writer-in-C-Part-1.html]
        /// [ref:http://knab.ws/blog/index.php?/archives/10-CSV-file-parser-and-writer-in-C-Part-2.html]
        /// CSV files have a very simple structure:
        /// > Each record is one line (with exceptions)
        /// > Fields are separated with commas
        /// > Leading and trailing space-characters adjacent to comma field separators are ignored
        /// > Fields with embedded commas must be delimited with double-quote characters
        /// > Fields that contain double quote characters must be surounded by double-quotes, and the embedded double-quotes must each be represented by a pair of consecutive double quotes.
        /// > A field that contains embedded line-breaks must be surounded by double-quotes
        /// > Fields with leading or trailing spaces must be delimited with double-quote characters
        /// > Fields may always be delimited with double quotes
        /// > The first record in a CSV file may be a header record containing column (field) names
        /// </summary>
        /// <param name="s"></param>
        /// <param name="D"></param>
        /// <param name="data"></param>
        public void ParseCsvString(String s, Char D, ref List<String> data)
        {
            // quoted strings allows us to include the delimiter without 
            // an escape character.
            Boolean quoted = false;
            // white space before and after will be ignored if an item
            // contains double quotes
            Boolean predata = true;
            Boolean postdata = false;
            Boolean EOS = (s.Length == 0);
            Boolean EOL = (s.Length == 0);
            StringBuilder item = new StringBuilder();
            Char c = ' ';
            Int16 pos = 0;
            while (!EOS && !EOL)
            {
                c = s[pos];
                if (EOS)
                {
                    data.Add(item.ToString());
                    break;
                }
                if ((postdata || !quoted) && c == D)
                { // delimiter means end of this item, start of new
                    data.Add(item.ToString());
                    // reset item specific variables
                    quoted = false;
                    predata = true;
                    postdata = false;
                    item = new StringBuilder();
                    // next char
                    pos++;
                    EOS = (s.Length == pos);
                    continue;
                }

                if ((predata || postdata || !quoted) && (c == '\x0A' || c == '\x0D'))
                {
                    // End Of Line found - save item and stop processing line
                    EOL = true;
                    if (s.Length > (pos+1))
                    {
                        if (c == '\x0D' && s[(pos+1)] == '\x0A')
                        {
                            pos++;
                        }
                    }
                    data.Add(item.ToString());
                    // reset item specific variables
                    quoted = false;
                    predata = true;
                    postdata = false;
                    item = new StringBuilder();
                    // next char
                    pos++;
                    EOS = (s.Length == pos);
                    continue;
                }

                if (predata && c == ' ')
                { // ignore leading white space
                    // next char
                    pos++;
                    EOS = (s.Length == pos);
                    continue;
                }

                if (predata && c == '"')
                { // start of quoted string
                    quoted = true;
                    predata = false;
                    // next char
                    pos++;
                    EOS = (s.Length == pos);
                    continue;
                }

                if (predata)
                { // item starts without quotes
                    predata = false;
                    item.Append(c);
                    // next char
                    pos++;
                    EOS = (s.Length == pos);
                    continue;
                }

                if (c == '"' && quoted)
                {
                    if (s.Length > (pos+1))
                    {
                        if (s[(pos+1)] == '"')
                        { // save first double quote
                            item.Append(c);
                            pos++; // move to next double quote
                        }
                        else
                        { // do not save end quote
                            postdata = true;
                        }
                        // next char
                        pos++;
                        EOS = (s.Length == pos);
                        continue;
                    }
                }

                // save character
                item.Append(c);
                // next char
                pos++;
                EOS = (s.Length == pos);
                continue;
            }
            if (EOS)
            { // add the last item
                data.Add(item.ToString());
            }
        }

        /// <summary>
        /// Parse a string into multiple strings using a delimiter
        /// Allows for an escape character.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ESC"></param>
        /// <param name="D"></param>
        /// <param name="data"></param>
        public void ParseString(String s, Char ESC, Char D, ref List<String> data)
        {
            data.Clear();
            // strings use a zero based index
            Int32 lidx = 0; // last save index 
            Int32 sidx = 0; // initial index 
            Int32 idx = 0; // found delimiter
            String str = String.Empty;
            while (idx >= 0)
            {
                idx = s.IndexOf(D, sidx);
                if (idx >= 0)
                {
                    if (idx == sidx) 
                    {
                        // null length value
                        str += s.Substring(sidx, idx - sidx);
                        data.Add(str);
                        str = String.Empty;
                        lidx = idx + 1;
                        sidx = lidx;
                    }
                    else if (s[idx - 1] == ESC)
                    {
                        // escape'd character
                        // so ignore this delimiter and try again
                        str += s.Substring(sidx, idx - sidx + 1);
                        sidx = idx + 1;
                    }
                    else
                    {
                        // save string
                        str += s.Substring(sidx, idx - sidx);
                        data.Add(str);
                        str = String.Empty;
                        // move past delimiter character
                        lidx = idx + 1;
                        sidx = lidx;
                    }
                }
                else
                {
                    // could not find character
                    // save what is left
                    str += s.Substring(sidx);
                    data.Add(str);
                    str = String.Empty;
                }
            }
        }


        /// <summary>
        /// Escape all special characters using the given escape character.
        /// </summary>
        /// <param name="str">String to be escaped</param>
        /// <param name="ESC">Escape character</param>
        /// <param name="SpecialChars">Array of Special Characters to be escaped</param>
        /// <returns>String containing escape characters</returns>
        public String EscapeString(String str, Char ESC, Char[] SpecialChars)
        {
            String ret = str;

            String r = String.Empty;
            r += ESC;
            r += ESC;
            String o = String.Empty;
            o += ESC;
            ret = ret.Replace(o, r);

            foreach(Char c in SpecialChars)
            {
                String repl = String.Empty;
                repl += ESC;
                repl += c;
                String orig = String.Empty;
                orig += c;
                ret = ret.Replace(orig,repl);
            }
            return ret;
        }

        /// <summary>
        /// Escape all special characters using the given escape character.
        /// </summary>
        /// <param name="str">String to be escaped</param>
        /// <param name="ESC">Escape character</param>
        /// <param name="SpecialChars">Array of Special Characters to be escaped</param>
        /// <returns>String containing escape characters</returns>
        public String UnEscapeString(String str, Char ESC)
        {
            String ret = String.Empty;

            int sidx = 0; //start of search
            int idx = 0; // last search result
            idx = str.IndexOf(ESC, sidx);
            while (idx != -1)
            {
                if (idx == sidx)
                { // start of search is an escape
                    if (str.Length > idx + 1)
                    { // save character after escape character
                        sidx++; // skip escape character
                        ret += str.Substring(sidx, 1);
                        sidx++; // start at next character
                    }
                    else
                    {
                        // no more data? but we have an escape character?
                        break;
                    }
                }
                else if (idx > sidx)
                {
                    if (str.Length >= idx + 1)
                    {
                        ret += str.Substring(sidx, idx - sidx);
                        sidx = ++idx; // skip ESC character
                        if (sidx < str.Length)
                        {
                            ret += @str[sidx];
                        }
                        sidx++; // start at next character
                    }
                    else
                    {
                        // found something past the end of string
                    }
                }
                if (str.Length > sidx + 1)
                {
                    idx = str.IndexOf(ESC, sidx);
                }
                else
                {
                    break;
                }
            }
            // copy remaining characters
            if (sidx < str.Length)
            {
                ret += str.Substring(sidx);
            }
            return ret;
        }

        /// <summary>
        /// Register the type of an object that is serialized
        /// A type is registered against its full classname
        /// including namespace but not the assembly.
        /// 
        /// Child text formatter classes can override this
        /// method and the ObjectType(data) method to use their
        /// own registration method for types.
        /// </summary>
        /// <param name="t">System.Type returned by call typeof(T)</param>
        public virtual void RegisterType(Type t)
        {
            if (!m_RegisteredTypes.ContainsKey(t.FullName))
            {
                m_RegisteredTypes.Add(t.FullName, t);
            }
            else
            {
                m_RegisteredTypes[t.FullName] = t;
            }
        }

        /// <summary>
        /// Register a name mapping for a given type.
        /// Name mappings are used to deserialize a stream
        /// in ValueOnly storage mode into a meaningful set
        /// of name/value pairs.
        /// 
        /// Also registers the Type if not already registered.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="map"></param>
        public virtual void RegisterNameMap(Type t, List<String> map)
        {
            if (!m_RegisteredTypes.ContainsKey(t.FullName))
            {
                RegisterType(t);
            }
            m_RegisteredNameMaps.Add(t.FullName, map);
        }

        /// <summary>
        /// After reading a line from an input stream the stream is separated
        /// into name-value pairs using the assigned delimiters to break
        /// the line into individual items.  Default delimiters are a comma ','
        /// for separating pairs and equal '=' to separate name from value.
        /// Values will be an empty string when empty or not specified.
        /// Eg. Name1=abc,Name2=def,....
        /// </summary>
        /// <param name="nameValuePairs"></param>
        /// <returns></returns>
        public virtual System.Type ObjectType(Dictionary<String,String> nameValuePairs)
        {
            Type t = typeof(Object); // default
            // use the classname to find the relevant type
            String classname = String.Empty;
            if (m_StorageType == StorageType.NameValue)
            {
                if (nameValuePairs.ContainsKey("SYS_ClassFullName"))
                {
                    classname = nameValuePairs["SYS_ClassFullName"];
                }
                if (m_RegisteredTypes.ContainsKey(classname))
                {
                    t = m_RegisteredTypes[classname];
                }
            }
            else if (m_StorageType == StorageType.ValueOnly
                || m_StorageType == StorageType.CSV)
            {
                if (nameValuePairs.Count > 0)
                {
                    // first value
                    UInt32 cnt = 0;
                    String key = cnt.ToString(m_NameIndexFormat);
                    if (nameValuePairs.ContainsKey(key))
                    {
                        classname = nameValuePairs[key];
                    }
                }
            }
            return t;
        }

        /// <summary>
        /// Return the name mapping for a specific Type.
        /// Name mappings refer to the ValueOnly storage mode
        /// where the order in which values are read are mapped
        /// to a specific field name when populating the 
        /// SerializationInfo object used for deserialization.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual List<String> ObjectNameMap(Type t)
        {
            List<String> ret = null;
            if (m_RegisteredNameMaps.ContainsKey(t.FullName))
            {
                ret = m_RegisteredNameMaps[t.FullName];
            }
            else
            {
                if (UseFirstLineAsColumnNames)
                { // first line must be read using this formatter
                    // all future lines are assumed to use the names 
                    // from the first line.
                    ret = m_FirstLineColumnNames;
                }
            }
            return ret;
        }

        /// <summary>
        /// Parse a string into name/value pairs using the 
        /// defined delimiter characters.
        /// 
        /// Child text formatter classes can override this
        /// method to parse an input string using their own
        /// parsing method to populate the data dictionary 
        /// with values.  The data dictionary will be used 
        /// to determine which object instance to create.
        /// See ObjectType(data)
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="ESC">Escape character for delimiters</param>
        /// <param name="ND">Name delimiter</param>
        /// <param name="VD">Name-Value delimiter</param>
        /// <param name="data">Container to fill with parsed data</param>
        public virtual void DeserializeFromString(
            String s, Char ESC, Char ND, Char VD
            , ref Dictionary<String, String> data)
        {
            data.Clear();
            List<String> pairs = new List<string>();
            if (m_StorageType == StorageType.CSV)
            {
                ParseCsvString(s, ND, ref pairs);
            }
            else
            {
                ParseString(s, ESC, ND, ref pairs);
            }
            UInt32 cnt = 0;
            foreach (String str in pairs)
            {
                if (m_StorageType == StorageType.NameValue)
                {
                    List<String> nv = new List<string>();
                    ParseString(str, ESC, VD, ref nv);
                    if (nv.Count > 0)
                    {
                        if (nv.Count == 1)
                        { // no value specified
                            String nv0 = UnEscapeString(nv[0], ESC);
                            data.Add(nv0, "");
                        }
                        else if (nv.Count >= 2)
                        { // only take the first value if multiple exist
                            String nv0 = UnEscapeString(nv[0], ESC);
                            String nv1 = UnEscapeString(nv[1], ESC);
                            data.Add(nv0, nv1);
                        }
                    }
                }
                else
                { // ValueOnly and CSV
                    String val = UnEscapeString(str, ESC);
                    data.Add(cnt.ToString(m_NameIndexFormat), val);
                    cnt++;
                }
            }
        }

        /// <summary>
        /// Read a single line of text from a stream, converting the 
        /// bytes into UTF8 encoded text.
        /// 
        /// Does not read the entire stream like the StreamReader.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public String ReadLine(Stream s)
        {
            String ret = String.Empty;
            if (s.CanRead && s.Position < s.Length)
            {
                // read string one byte at a time until eof or eol
                byte[] sdata = new byte[10];
                int offsetIntoByteArray = 0;
                int charsToRead = 1;
                int cnt = s.Read(sdata, offsetIntoByteArray, charsToRead);
                if (cnt > 0)
                {
                    char[] cdata = Encoding.UTF8.GetChars(sdata);
                    // search for a new line or no more characters to read
                    while (cnt > 0 && cdata[0] != '\n')
                    {
                        if (cdata.GetLength(0) > 0)
                        {
                            for (Int32 x = 0; x <= offsetIntoByteArray; ++x)
                            {
                                // ignore carriage return
                                if (cdata[x] != '\r')
                                {
                                    ret += cdata[x];
                                }
                            }
                            offsetIntoByteArray = 0;
                        }
                        else
                        {
                            offsetIntoByteArray += cnt;
                        }
                        cnt = s.Read(sdata, offsetIntoByteArray, charsToRead);
                        if (cnt > 0)
                        {
                            cdata = Encoding.UTF8.GetChars(sdata);
                        }
                    }
                }
            }
            m_LastLineRead = ret;
            return ret;
        }

        /// <summary>
        /// Deserialize one class from a given stream.
        /// The stream MUST have one line per Object. Which 
        /// is what happens when this formatter was used 
        /// to create the stream.
        /// </summary>
        /// <param name="s">Stream to deserialize</param>
        /// <returns></returns>
        public Object Deserialize(Stream s)
        {
            Object ret = null;
            // read one line from the stream that defines our object
            m_FirstLine = false;
            if (s.CanRead && s.Position == 0)
            {
                m_FirstLine = true;
            }
            String str = ReadLine(s);
            
            // local storage of the elements read from the line
            Dictionary<String, String> data
                = new Dictionary<string, string>();

            DeserializeFromString(str, m_EscapeChar
                , m_NameDelimiter, m_ValueDelimiter, ref data);
            try
            {
                if (m_StorageType == StorageType.CSV
                    && UseFirstLineAsColumnNames && m_FirstLine)
                { // create name map for this stream
                    foreach (KeyValuePair<String, String> value in data)
                    {
                        m_FirstLineColumnNames.Add(value.Value);
                    }
                    // return a copy of the name list
                    Type t = m_FirstLineColumnNames.GetType();
                    Type[] types = { };
                    Object[] objs = { };
                    ret = t.GetConstructor(types).Invoke(objs);
                }
                else
                {
                    // find relevant class to create and call its 
                    // serialization constructor
                    Type t = ObjectType(data);

                    if (t != typeof(Object))
                    {
                        // create a different SerializationInfo for each object
                        SerializationInfo info
                            = new SerializationInfo(t, m_Converter);
                        // add the name/value pairs to the SerializationInfo object
                        if (m_StorageType == StorageType.NameValue)
                        {
                            foreach (KeyValuePair<String, String> value in data)
                            {
                                info.AddValue(value.Key, value.Value);
                            }
                        }
                        else if (m_StorageType == StorageType.ValueOnly
                            || m_StorageType == StorageType.CSV)
                        {
                            List<String> nameMap = ObjectNameMap(t);

                            IEnumerator<String> nameItr = null;
                            if (nameMap != null)
                            {
                                // starts before first item - MoveNext() to set at first item
                                nameItr = nameMap.GetEnumerator();
                            }
                            foreach (KeyValuePair<String, String> value in data)
                            {
                                if (nameMap != null)
                                {
                                    String key = String.Empty;
                                    if (nameItr.MoveNext())
                                    {
                                        if (nameItr.Current != null)
                                        {
                                            key = nameItr.Current;
                                        }
                                    }
                                    // an empty key from the name map
                                    // indicates to use the auto-generated key
                                    if (key.Length > 0)
                                    {
                                        info.AddValue(key, value.Value);
                                    }
                                    else
                                    {
                                        info.AddValue(value.Key, value.Value);
                                    }
                                }
                                else
                                { // use the auto-generated key
                                    info.AddValue(value.Key, value.Value);
                                }
                            }
                        }

                        // call private constructor for ISerializable objects
                        Type[] types = { info.GetType(), m_Context.GetType() };
                        Object[] objs = { info, m_Context };
                        ret = t.GetConstructor(types).Invoke(objs);
                    }
                    else
                    {
                        // no type defined by the deserialized string
                    }
                }
            }
            catch (SerializationException e)
            {
                throw e;
            }
            
            // return instance of new object
            return ret;
        }

        /// <summary>
        /// Deserialize to a specific type.
        /// This call bypasses the call to ObjectType(data)
        /// and uses the object type "t".
        /// </summary>
        /// <param name="s">Stream to deserialize</param>
        /// <param name="t">Object type to be created</param>
        /// <returns></returns>
        public Object Deserialize(Stream s, Type t)
        {
            Object ret = null;
            // read one line from the stream that defines our object
            m_FirstLine = false;
            if (s.CanRead && s.Position == 0)
            {
                m_FirstLine = true;
            }
            String str = ReadLine(s);

            // local storage of the elements read from the line
            Dictionary<String, String> data
                = new Dictionary<string, string>();

            DeserializeFromString(str, m_EscapeChar
                , m_NameDelimiter, m_ValueDelimiter, ref data);
            try
            {
                if (m_StorageType == StorageType.CSV
                    && UseFirstLineAsColumnNames && m_FirstLine)
                { // create name map for this stream
                    foreach (KeyValuePair<String, String> value in data)
                    {
                        m_FirstLineColumnNames.Add(value.Value);
                    }
                    // return a copy of the name list
                    t = m_FirstLineColumnNames.GetType();
                    Type[] types = { };
                    Object[] objs = { };
                    ret = t.GetConstructor(types).Invoke(objs);
                }
                else if (t != typeof(Object))
                {
                    // create a different SerializationInfo for each object
                    SerializationInfo info
                        = new SerializationInfo(t, m_Converter);
                    // add the name/value pairs to the SerializationInfo object
                    if (m_StorageType == StorageType.NameValue)
                    {
                        foreach (KeyValuePair<String, String> value in data)
                        {
                            info.AddValue(value.Key, value.Value);
                        }
                    }
                    else if (m_StorageType == StorageType.ValueOnly
                        || m_StorageType == StorageType.CSV)
                    {
                        List<String> nameMap = ObjectNameMap(t);
                        IEnumerator<String> nameItr = null;
                        if (nameMap != null)
                        {
                            // starts before first item - MoveNext() to set at first item
                            nameItr = nameMap.GetEnumerator();
                        }
                        foreach (KeyValuePair<String, String> value in data)
                        {
                            if (nameMap != null)
                            {
                                String key = String.Empty;
                                if (nameItr.MoveNext())
                                {
                                    if (nameItr.Current != null)
                                    {
                                        key = nameItr.Current;
                                    }
                                }
                                // an empty key from the name map
                                // indicates to use the auto-generated key
                                if (key.Length > 0)
                                {
                                    info.AddValue(key, value.Value);
                                }
                                else
                                {
                                    info.AddValue(value.Key, value.Value);
                                }
                            }
                            else
                            { // use the auto-generated key
                                info.AddValue(value.Key, value.Value);
                            }
                        }
                    }

                    // call private constructor for ISerializable objects
                    Type[] types = { info.GetType(), m_Context.GetType() };
                    Object[] objs = { info, m_Context };
                    ret = t.GetConstructor(types).Invoke(objs);
                }
                else
                {
                    // no type defined by the deserialized string
                }
            }
            catch (SerializationException e)
            {
                throw e;
            }
            // return instance of new object
            return ret;
        }

        /// <summary>
        /// Serialize an object to a stream.
        /// Objects are serialized only if they have the Serializable attribute
        /// and inherit from ISerializable.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void Serialize(Stream s, Object o)
        {
            try
            {
                // create a different SerializationInfo for each object
                System.Type t = o.GetType();
                SerializationInfo info
                    = new SerializationInfo(t, m_Converter);
                String classname = t.FullName;

                // first check if there is a serialization surrogate
                // for the object type
                ISerializationSurrogate surrogate = null;
                if (m_Selector != null)
                {
                    surrogate = m_Selector.GetSurrogate(t, m_Context, out m_Selector);
                }
                if (surrogate != null)
                {
                    // is this object serializable?
                    if (o is ISerializable)
                    {
                        surrogate.GetObjectData(o, info, m_Context);
                    }
                    else
                    {
                        // surrogate requires object to inherit from
                        // ISerializable interface
                        throw new SerializationException(
                            "Object is not serializable: " + t.FullName);
                    }
                }
                else
                {
                    // is this object serializable?
                    if (o is ISerializable)
                    {
                        ISerializable so = o as ISerializable;
                        so.GetObjectData(info, m_Context);
                    }
                    else
                    {
                        // we require objects to inherit from
                        // ISerializable interface
                        throw new SerializationException(
                            "Object is not serializable: " + t.FullName);
                    }
                }
                // now serialize the data to the stream
                // for each info value write to the stream
                // starting with the full class name
                String str = SerializeToString(info, t);
                //write the line to the stream
                StreamWriter sw = new StreamWriter(s);
                sw.WriteLine(str.ToString());
                sw.Flush();
            }
            catch (SerializationException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Child text formatter classes can create their own 
        /// method to build the string to be written to the 
        /// stream.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual String SerializeToString(SerializationInfo info, System.Type t)
        {
            StringBuilder str = new StringBuilder();
            if (m_StorageType == StorageType.NameValue)
            {
                str.AppendFormat("{0}{1}{2}"
                        , "SYS_ClassFullName", m_ValueDelimiter, t.FullName);
            }
            else if (m_StorageType == StorageType.ValueOnly)
            {
                str.AppendFormat("{0}", t.FullName);
            }
            foreach (SerializationEntry e in info)
            {
                if (m_StorageType == StorageType.NameValue)
                {
                    if (e.Name != null)
                    {
                        String name = EscapeString(e.Name, m_EscapeChar
                            , new Char[] { m_NameDelimiter, m_ValueDelimiter });
                        if (e.Value != null)
                        {
                            String value = EscapeString(e.Value.ToString(), m_EscapeChar
                                , new Char[] { m_NameDelimiter, m_ValueDelimiter });
                            str.AppendFormat("{0}{1}{2}{3}"
                                , m_NameDelimiter, name
                                , m_ValueDelimiter, value);
                        }
                        else
                        {
                            str.AppendFormat("{0}{1}{2}{3}"
                                , m_NameDelimiter, name
                                , m_ValueDelimiter, String.Empty);
                        }
                    }
                }
                else if (m_StorageType == StorageType.ValueOnly)
                {
                    if (e.Value != null)
                    {
                        String value = EscapeString(e.Value.ToString(), m_EscapeChar
                            , new Char[] { m_NameDelimiter });
                        str.AppendFormat("{0}{1}"
                            , m_NameDelimiter, value);
                    }
                }
                else if (m_StorageType == StorageType.CSV)
                {
                    if (e.Value != null)
                    {
                        String value = e.Value.ToString();
                        if(QuoteAll || value.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
                        {
                            value = "\"" + value.Replace("\"", "\"\"") + "\"";
                        }
                        str.AppendFormat("{0}{1}"
                            , m_NameDelimiter, value);
                    }
                }
            }
            return str.ToString();
        }

        /// <summary>
        /// Binder is currently ignored by this class
        /// </summary>
        public SerializationBinder Binder
        {
            get { return m_Binder; }
            set { m_Binder = value; }
        }

        /// <summary>
        /// Indicates the source/destination stream type
        /// </summary>
        public StreamingContext Context
        {
            get { return m_Context; }
            set { m_Context = value; }
        }

        /// <summary>
        /// Type specific selection of a serialization object
        /// that can de/serialize a given type. The object must
        /// inherit from ISerializationSurrogate.  This facility 
        /// is used to serialize objects that do not have the 
        /// Serializable attribute or inherit from ISerializable.
        /// </summary>
        public ISurrogateSelector SurrogateSelector
        {
            get { return m_Selector; }
            set { m_Selector = value; }
        }
    
    }

}
