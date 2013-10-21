namespace CommonLib.CDEF
{
	public enum AddressType : uint
	{
		Bravo							= 1,
		BravoAdmin						= 2,
		BravoInformation				= 5,
		SoftwareDownloadHost			= 10,
		Wis								= 12,
		TrackSide						= 14,
		Sportsbet						= 16,
		Cosmos							= 18,
		OutletGateway					= 20,
		HostGateway						= 21,
		InternetBettingSystemGateway	= 23,
		ForeignHostGateway				= 24,
		GenericSellingTerminal			= 25,
		GenericDisplayTerminal			= 26,
		GenericAdminTerminal			= 27
	}

	public enum DeviceType : byte
	{
		Reserved				= 0x00,
		BravoAdmin				= 0x01,
		BravoWagering			= 0x02,
		BravoDownload			= 0x03,
		Wis						= 0x04,
		BravoSlaveAdmin			= 0x05,
		BravoSlaveWagering		= 0x06,
		BravoSlaveDownload		= 0x07,
		ADC						= 0x41,
		CIT						= 0x43,
		EBT						= 0x45,
		FEPC					= 0x46,
		GenericTerminal			= 0x47,
		RetailGateway			= 0x48,
		IBI						= 0x49,
		FHG						= 0x4E,
		ODC						= 0x4F,
		PWT						= 0x50,
		RWT						= 0x52,
		T2290					= 0x54,
		OutletGateway			= 0x59,
		InternetBettingSystem	= 0x69,
		AnyDevice				= 0xFF
	}

	public enum ProtocolType : byte
	{
		UnKnown		= 0,
		Application	= 1,
		Session		= 2,
		Network		= 3
	}

	public enum SessionDescriptorType : uint
	{
		Wagering	= 0,
		Undefined_1	= 1,
		Undefined_2	= 2,
		Undefined_3	= 3
	}
}