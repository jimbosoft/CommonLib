using System;

namespace CommonLib.CDEF
{
	public enum FuncCode : short
	{
		Heartbeat									= 0x0310,
        ListActiveClubs                             = 0x0319,
		CostInquiry									= 0x0404,
		MysteryBet									= 0x0409,
        Cancel_Transaction                          = 0x0415,
		Trackside_PrintResultsTicket				= 0x0419,
		Trackside_GameStatus						= 0x041A,
		Trackside_GameNumbersInquiry				= 0x041B,
		Sign_On										= 0x0603,
        Ad_Sign_On                                  = 0x0630,
        Sign_Off                                    = 0x0604,
        CMS_Message                                 = 0x0634,
        OperatorBalance                             = 0x060B,
		AccountBalance								= 0x0D10,
		LastBet										= 0x0D11,
        CombPayRedeem                               = 0x0D18,
		AccessAccount								= 0x0D0B,
        EndOfRacedayNotify                          = 0x0D4F,
        CashIn                                      = 0x0E03,
        CashOut                                     = 0x0E04,
        OCMMeetingTotal                             = 0x0E07,
        OCMOpenCloseCash                            = 0x0E08,
        TransactionLocation                         = 0x1319,
		VIA											= 0x131A,
		RacedayID									= 0x1322,
		RLDB_SNQuery								= 0x1608,
		RLDB_MeetingHeadingsSN						= 0x1609,
		RLDB_RaceHeadingsSN							= 0x160A,
		RLDB_ContestantsSN							= 0x160B,
		RLDB_MeetingInformationSentSN				= 0x160C,
		RLDB_MultilegDividendsSN					= 0x160D,
		RLDB_TrackNWeatherSN						= 0x160E,
		RLDB_MultilegApproximatesSN					= 0x160F,
		RLDB_CancelMeetingSN						= 0x1610,
		RLDB_AbandonEventSN							= 0x1611,
		RLDB_ReinstateEventSN						= 0x1612,
		RLDB_AmendEventStartTime					= 0x1613,
		RLDB_CloseSellSN							= 0x1614,
		RLDB_OpenSellSN								= 0x1615,
		RLDB_EventSubstituteSN						= 0x1616,
		RLDB_WinPlaceApproximatesSN					= 0x1617,
		RLDB_MultiPositionalEventsApproximatesSN	= 0x1618,
		RLDB_EventResultsSN							= 0x1619,
		RLDB_EventDividendsSN						= 0x161A,
		RLDB_AnnounceDividendsSN					= 0x161B,
		RLDB_ScratchingsReinstatementsSN			= 0x161C,
		RLDB_FinalScratchingsSN						= 0x161D,
		RLDB_ContestantsRSBSN						= 0x161E,
		RLDB_RequestPreviousDaysResults				= 0x161F,
		RLDB_PreviousDaysResults					= 0x1620,
		RLDB_RequestResultsSNRange					= 0x1621,
		RLDB_PreviousDaysResultsData				= 0x1622,
		RLDB_RequestUnsequenced						= 0x1623,
		RLDB_SNNotification							= 0x1624,
		RLDB_SNRequest								= 0x1625,
		RLDB_MysteryProductDividend					= 0x1626,
		RLDB_MultilegApproximatesWithJackpot		= 0x1629,
		RLDB_ApproxTimeToJump						= 0x162B,
		RLDB_PreviousDaysResultsEx					= 0x16E0,
		RLDB_ReqMultiplePrevDaysResults				= 0x16E1,
		File_StatusRequest							= 0x1801,
		File_StatusResponse							= 0x1802,
		File_DownloadRequest						= 0x1803,
		File_DownloadData							= 0x1804,
		File_UploadRequest							= 0x1805,
		File_UploadData								= 0x1806,
		File_Delete									= 0x1807,
		Wisnet_GetConfigValues						= 0x1808,
		Wisnet_SetConfig							= 0x1809,
		Wisnet_GetConfigKeys						= 0x180A,
		Add_FileAuditRecord							= 0x180B,
		Get_FileAuditRecord							= 0x180C,
		File_DeleteFileset							= 0x180D,
		File_FilesetStatus							= 0x180E,
		Wisnet_InternalCommand						= 0x180F,
		Wisnet_ApplicationState						= 0x1810,
		Wisnet_Endpoint								= 0x1811,
		Wisnet_TimeToSend							= 0x1812,
		Wisnet_SetThrottle							= 0x1813,
		Wisnet_RcySkyIndentification				= 0x1814,
		Wisnet_SkyMasterStatus						= 0x1815,
		Wisnet_SkyFlush								= 0x1816,
		Wisnet_OptionRequest						= 0x1817,
		SB_InformationUpdate						= 0x3103,
		SB_SNNotification							= 0x3106,
		SB_SNQuery									= 0x3107,
		SB_SNRequest								= 0x3108,
		SBR_SNResult								= 0x3180,
		SBR_SNCancelResult							= 0x3181,
		SBR_PreviousDaysResults						= 0x31A0,
		SBR_SNNotificationRequest					= 0x31D0,
		SBR_SNNotification							= 0x31D1,
		SBR_RequestPreviousDaysResults				= 0x31D2,
		SBR_PreviousDaysResultsSNRange				= 0x31D3,
		SBR_TodaysSNQuery							= 0x31D4,
		SBR_RequestPreviousDaysResultsSNRange		= 0x31D5,
		Trackside_TodaysSNQuery						= 0x3208,
		Trackside_ResultSN							= 0x3219,
		Trackside_RequestPreviousDaysResults		= 0x321F,
		Trackside_PreviousDaysResultsSNRange		= 0x3220,
		Trackside_RequestPreviousDaysResultsSNRange	= 0x3221,
		Trackside_PreviousDaysResults				= 0x3222,
		Trackside_SNNotification					= 0x3224,
		Trackside_SNNotificationRequest				= 0x3225,
		Network_Establishment_Device_Notification	= 0x3306,
		Network_Establishment_Get_Entity_IPAddr		= 0x3307,
		Session_Establishment_Connect				= 0x3308,
		Session_Establishment_Disconnect			= 0x3309,
		Link_Capacity								= 0x3310,
		Flow_Control_State							= 0x3311,
		Flow_Control_Query							= 0x3312,
		Network_Establishment_Get_Broadcast_Settings= 0x3313,
        StartOfRacedayNotify                        = 0x3316,
		FR_RequestMultiplePreviousRoundsResults		= 0x3401,
		FR_PreviousDaysResultsSNRange				= 0x3402,
		FR_RequestResultsSNRange					= 0x3403,
		FR_PreviousDaysResultData					= 0x3404,
		FR_ResultSN									= 0x3405,
		FR_RoundInformationSummary					= 0x3406,
		FR_TodaySeqNotification						= 0x3408,
		FR_TodaySeqNotificationReq					= 0x3409,
		FR_TodaySeqNumQuery							= 0x340A,
        EndOfOncourceMeetingNotify                  = 0x3806,
        Notify                                      = 0x4002,
		SellTicket									= 0x0403,
        TracksideSellTicket                         = 0x0416,
		IBI_Message_Packet							= 0x5010,
		IBS_System_Status							= 0x5100
	}

	public enum SportsbetUpdateType : byte
	{
		Reserved		= 0x00,
		SConfig			= 0x01,
		DConfig			= 0x02,
		UpdateRecord	= 0x03
	}

	public enum eTierNumber : byte
	{
		TIER_ONE						= 1,
		TIER_TWO						= 2,
		TIER_THREE						= 3,
		TIER_FOUR						= 4
	}

	public enum eWPC : byte // Wagering Product Code
	{
		WPC_NONE						= 0,
		WPC_WIN							= 1,
		WPC_PLACE						= 2,
		WPC_TRIFECTA					= 3,
		WPC_DAILY_DOUBLE				= 4,
		WPC_EXTRA_DOUBLE				= 5,
		WPC_SPECIAL_DOUBLE				= 6,
		WPC_QUINELLA					= 8,
		WPC_QUADDIE						= 9,
		WPC_RUNNING_DOUBLE				= 10,
		WPC_EXACTA						= 11,
		WPC_EACH_WAY					= 12,
		WPC_QUADXTRA					= 13,
		WPC_STRAIGHT6					= 14,
		WPC_FOOTY_PICK6					= 15,
		WPC_TRIO						= 16,
		WPC_TREBLE						= 17,
		WPC_QUARTET						= 18,
		WPC_QUINTET						= 19,
		WPC_SEXTET						= 20,
		WPC_TWIN_TRIFECTAS				= 21,
		WPC_QUINELLA_DOUBLE				= 22,
		WPC_PINK_N_LEGS					= 23,
		WPC_PICK_N_EVENT_ANY			= 24,
		WPC_PICK_N_EVENT_CORRECT		= 25,
		WPC_PICK8						= 26,
		WPC_PICK7						= 27,
		WPC_SPECIAL_FOOTY_EXTRA_DOUBLE	= 28,
		WPC_SPECIAL_FOOTY_QUAD			= 29,
		WPC_HEAD_TO_HEAD				= 30,
		WPC_POINTS_SPREAD				= 31,
		WPC_MARGIN						= 32,
		WPC_DUET						= 33,
		WPC_MYSTERY6					= 34,
		WPC_PICK_THE_WINNERS			= 35,
		WPC_PICK_THE_MARGINS			= 36,
		WPC_PICK_THE_SCORES				= 37,
		WPC_FIRST_FOUR					= 38,
		WPC_THREE_UP					= 39,
		WPC_ACCUMULATOR					= 40,
		WPC_MULTI_BET					= 41,
		WPC_RACING_DOUBLES				= 42,

		WPC_CUP_PACK					= 128,
		WPC_SUMMER_PACK					= 129
	}

	public enum ePoolStatus : byte
	{
		Closed				= 0x43, //67
		Open				= 0x4f  //79
	}

	public enum eScratchStatus : byte
	{
		Emergency			= 0x45,	//69
		ScratchedEmergency	= 0x46,	//70
		Late				= 0x4c,	//76
		NotScratched		= 0x4e,	//78
		ReInstated			= 0x52,	//82
		Scratched			= 0x53	//83
	}

	public enum eMeetingStatus : byte
	{
		Abandoned			= 0x41, //65
		Complete			= 0x43, //67
		Closed				= 0x4C,	//76
		Open				= 0x4f, //79
		Postponed			= 0x50  //80
	}

	public enum eEventStatus : byte
	{
		Abandoned			= 0x41, //65
		Closed				= 0x43, //67
		Final				= 0x46, //70
		Interim				= 0x49, //73
		Open				= 0x4f, //79
		Protest				= 0x50, //80
		Postponed			= 0x54  //84
	}

	public enum eDividendStatus : byte
	{
		Final				= 0x46,	//70
		Interim				= 0x49	//73
	}

	public enum eVenueCode : byte
	{
		VENUE_CODE_NONE					= 0,
		VENUE_CODE_MELBOURNE			= 1,
		VENUE_CODE_PROVINCIAL1			= 2,
		VENUE_CODE_PROVINCIAL2			= 3,
		VENUE_CODE_SYDNEY				= 4,
		VENUE_CODE_ADELAIDE				= 5,
		VENUE_CODE_BRISBANE				= 6,
		VENUE_CODE_PERTH				= 7,
		VENUE_CODE_EXTRA2				= 8,
		VENUE_CODE_EXTRA3				= 9,
		VENUE_CODE_EXTRA1				= 10,
		VENUE_CODE_PROVINCIAL3			= 11,
		VENUE_CODE_EXTRA4				= 12,
		VENUE_CODE_PROVINCIAL5			= 13,
		VENUE_CODE_PROVINCIAL4			= 14,
		VENUE_CODE_EXTRA5				= 15
	}

	public enum eTypeCode : byte
	{
		TYPE_CODE_NONE					= 0,
		TYPE_CODE_RACES					= 1,
		TYPE_CODE_HARNESS				= 2,
		TYPE_CODE_GREYHOUNDS			= 3,
		TYPE_CODE_AFL					= 4,
		TYPE_CODE_SOCCER				= 5,
		TYPE_CODE_BASKETBALL			= 6,
		TYPE_CODE_GOLF					= 7,
		TYPE_CODE_TENNIS				= 8,
		TYPE_CODE_CRICKET				= 9,
		TYPE_CODE_BASEBALL				= 10,
		TYPE_CODE_NFL					= 11,
		TYPE_CODE_RUGBY_LEAGUE			= 12,
		TYPE_CODE_RUGBY_UNION			= 13,
		TYPE_CODE_OLYMPICS				= 14,
		TYPE_CODE_BOXING				= 15,
		TYPE_CODE_ATHLETICS				= 16,
		TYPE_CODE_MOTOR_SPORTS			= 17,
		TYPE_CODE_YACHTING				= 18,
		TYPE_CODE_GENERAL				= 255
	}

	public enum Series : byte
	{
		AFLPreSeason	  = 0x01,
		AFLHomeAndAway	  = 0x02,
		AFLFinals		  = 0x03,
		NRLPreSeason	  = 0x04,
		NRLHomeAndAway	  = 0x05,
		NRLFinals		  = 0x06,
		NRLStateOfOrigin  = 0x07,
		NRLTestMatch	  =	0x08
	}

	public enum RoundNumber : byte
	{
		EliminationFinal	= 0xF9,
		QuarterFinal		= 0xFA,
		Final				= 0xFB,
		QualifyingFinal		= 0xFC,
		SemiFinal			= 0xFD,
		PreliminaryFinal	= 0xFE,
		GrandFinal			= 0xFF
	}

	public enum FootyPoolDescriptor : byte
	{
		Win				= 0x01,
		Double			= 0x02,
		XtraDouble		= 0x03,
		HalfDouble		= 0x04,
		XtraHalfDouble	= 0x05,
		Quad			= 0x06,
		QuarterQuad		= 0x07,
		Pick7			= 0x08,
		Pick8			= 0x09,
		PickTheWinners  = 0x0a,
		PickTheMargins  = 0x0b,
		PickTheScores   = 0x0c
	}

	public enum FootyResultStatus : byte
	{
		Winner		= 0x01,
		Loser		= 0x02,
		Draw		= 0x03,
		NoResult	= 0x04
	}

	public enum SystemFlags : byte
	{
		Request		= 0x00,
		Error		= 0x01,
		Training	= 0x02,
		NotFirst	= 0x04,
		NotLast		= 0x08,
		Encryption	= 0x10,
		Encrypted	= 0x20,
		FloodTest	= 0x40,
		Response	= 0x80
	}

	public enum BettingStatus : byte
	{
		Null		=	0x0,
		Closed		=	0x43,
		Open		=	0x4F,
		Suspended	=	0x53
	}
}
