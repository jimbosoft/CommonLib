using System;
using System.Threading;

namespace CommonLib.CDEF
{
	public class SessionDescriptor
	{
		public static uint			BroadcastDescriptor			= 0x3FFFFFFF;

		private const int			DESCRIPTOR_TYPE_SHIFT		= 30;
		private const int			SALES_SHIFT					= 18;
		private const int			ADDRESS_TYPE_SHIFT			= 11;

		private const uint			DESCRIPTOR_TYPE_MASK		= 0x00000003;
		private const uint			SALES_MASK					= 0x00000FFF;
		private const uint			ADDRESS_TYPE_MASK			= 0x0000007F;
		private const uint			UNIQUE_ADDRESS_MASK			= 0x000007FF;

		private uint				m_descriptor				= 0;

		public SessionDescriptor()
		{
		}

        public SessionDescriptor(uint salesLocation, uint addressType, uint uniqueAddress)
        {
            Init(0, salesLocation, addressType, uniqueAddress);
        }

		public SessionDescriptor(uint descriptorType, uint salesLocation, uint addressType, uint uniqueAddress)
		{
            Init(descriptorType, salesLocation, addressType, uniqueAddress);
		}
        private void Init(uint descriptorType, uint salesLocation, uint addressType, uint uniqueAddress)
        {
			this.DescriptorType = descriptorType;
			this.SalesLocation = salesLocation;
			this.AddressType = addressType;
			this.UniqueAddress = uniqueAddress;
        }
        public SessionDescriptor(uint descriptor)
        {
            m_descriptor = descriptor;
        }

		public uint Descriptor
		{
			set { m_descriptor = value; }
			get { return m_descriptor; }
		}

		public uint DescriptorType
		{
			set
			{
				m_descriptor &= ~(DESCRIPTOR_TYPE_MASK << DESCRIPTOR_TYPE_SHIFT);
				m_descriptor |= (value & DESCRIPTOR_TYPE_MASK) << DESCRIPTOR_TYPE_SHIFT;
			}

			get
			{
				return (m_descriptor >> DESCRIPTOR_TYPE_SHIFT) & DESCRIPTOR_TYPE_MASK;
			}
		}

		public uint SalesLocation
		{
			set
			{
				m_descriptor &= ~(SALES_MASK << SALES_SHIFT);
				m_descriptor |= (value & SALES_MASK) << SALES_SHIFT;
			}

			get
			{
				return (m_descriptor >> SALES_SHIFT) & SALES_MASK;
			}
		}

		public uint AddressType
		{
			set
			{
				m_descriptor &= ~(ADDRESS_TYPE_MASK << ADDRESS_TYPE_SHIFT);
				m_descriptor |= (value & ADDRESS_TYPE_MASK) << ADDRESS_TYPE_SHIFT;
			}

			get
			{
				return (m_descriptor >> ADDRESS_TYPE_SHIFT) & ADDRESS_TYPE_MASK;
			}
		}

		public uint UniqueAddress
		{
			set
			{
				m_descriptor &= ~UNIQUE_ADDRESS_MASK;
				m_descriptor |= (value & UNIQUE_ADDRESS_MASK);
			}

			get
			{
				return m_descriptor & UNIQUE_ADDRESS_MASK;
			}
		}

		public override string ToString()
		{
			return string.Format( this.SalesLocation.ToString() + "," 
                                + this.AddressType.ToString() + ","
                                + this.UniqueAddress);
		}
        public bool IsEqual(SessionDescriptor rhs)
        {
            if (rhs == null)
            {
                return false;
            }
            return (AddressType == rhs.AddressType)
                    && (SalesLocation == rhs.SalesLocation)
                    && (UniqueAddress == rhs.UniqueAddress);
        }
        public static uint GetSalesLocation(uint descriptor)
		{
			return (descriptor >> SALES_SHIFT) & SALES_MASK;
		}

		public static uint GetUniqueAddress(uint descriptor)
		{
			return (descriptor & UNIQUE_ADDRESS_MASK);
		}

		public static uint GetAddressType(uint descriptor)
		{
			return (descriptor >> ADDRESS_TYPE_SHIFT) & ADDRESS_TYPE_MASK;
		}
	}
}
