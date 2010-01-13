/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Core;
using System.Runtime.InteropServices;

namespace GitSharp.Platform.Windows
{

	public static class Win32
	{
		
		[DllImport("kernel32.dll")]
		private static extern bool GetVersionEx([In,Out] Win32VersionInfo osvi);
		
		[DllImport("Kernel32.dll")]
		private static extern bool GetProductInfo(int dwOSMajorVersion,
		                                           int dwOSMinorVersion, int dwSpMajorVersion,
		                                           int dwSpMinorVersion, out uint dwOSEdition);
		
		[DllImport("kernel32.dll")]
		private static extern void GetSystemInfo(ref Win32SystemInfo sysInfo);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);
		
		private static void GetProductInfo(Win32ProductInfo info)
		{
			GetProductInfo(info.dwOSMajorVersion, info.dwOSMinorVersion,
			               info.dwSpMajorVersion, info.dwSpMinorVersion,
			               out info.dwOSEdition);
		}
		
		[DllImport("kernel32.dll", EntryPoint="CreateSymbolicLinkW", CharSet=CharSet.Unicode)]
		private static extern int CreateSymbolicLink([In] string lpSymlinkFileName, [In] string lpExistingFileName, int dwFlags);
		
		[DllImport("kernel32.dll", EntryPoint="CreateHardLinkW", CharSet=CharSet.Unicode)]
		private static extern int CreateHardLink([In] string lpHardlinkFileName, [In] string lpExistingFileName, SecurityAttributes attribs);
		
		public static bool IsSymlinkSupported()
		{
			System.OperatingSystem os = Environment.OSVersion;
			if (os.Version.Major >= 6)
				return true;
			else 
				return false;
		}
		
		public static bool IsHardlinkSupported()
		{
			System.OperatingSystem os = Environment.OSVersion;
			if (os.Version.Major >= 6)
				return true;
			else 
				return false;
		}
		
		public static bool CreateSymlink(string symlinkFilename, string exisitingFilename, bool isSymlinkDirectory)
		{
			if (IsSymlinkSupported())
			{
				int success;
				
				if (isSymlinkDirectory)
					success = CreateSymbolicLink(symlinkFilename, exisitingFilename, 1);
				else
					success = CreateSymbolicLink(symlinkFilename, exisitingFilename, 0);
				
				if (success == 0)
					return true;
			}

			return false;
		}
		
		public static bool CreateHardlink(string hardlinkFilename, string exisitingFilename)
		{
			if (IsHardlinkSupported())
			{
				SecurityAttributes attribs = null;
				int success = CreateHardLink(hardlinkFilename, exisitingFilename, attribs);
				if (success == 0)
					return true;
			}
	
			return false;
		}
		
		public static PlatformObject Load()
		{
			Win32VersionInfo osvi = new Win32VersionInfo();
			osvi.dwOSVersionInfoSize = Marshal.SizeOf(osvi);
			GetVersionEx(osvi);
			
			Win32SystemInfo sysInfo = new Win32SystemInfo();
			GetSystemInfo(ref sysInfo);
			PlatformObject obj = new PlatformObject();
			obj.ClassName = null;
			obj.PlatformType = "Windows";
			
			System.OperatingSystem os = Environment.OSVersion;
			obj.Version = os.Version.Major + "."+os.Version.Minor;
			switch (os.Platform)
			{
				case PlatformID.Win32Windows:
					switch (os.Version.Major)
					{
						case 4:
							switch (os.Version.Minor)
							{
								case 0:
									if (osvi.szCSDVersion == "B" ||
									    osvi.szCSDVersion == "C")
									{
										obj.ClassName = "Windows.v95";
										obj.PlatformSubType = "95";
										obj.Edition = "OSR2";
									}
									else
									{
										obj.ClassName = "Windows.v95";
										obj.PlatformSubType = "95";
										obj.Edition = "";
									}
									break;
								case 10:
									if (osvi.szCSDVersion == "A")
									{
										obj.ClassName = "Windows.v98";
										obj.PlatformSubType = "98";
										obj.Edition = "SE";
									}
									else
									{
										obj.ClassName = "Windows.v98";
										obj.PlatformSubType = "98";
										obj.Edition = "";
									}
									break;
								case 90:
									obj.ClassName = "Windows.ME";
									obj.PlatformSubType = "ME";
									obj.Edition = "";
									break;
							}
							break;
					}
					
					break;
				case PlatformID.Win32NT:
					switch (os.Version.Major)
					{
						case 3:
							obj.ClassName = "Windows.NT";
							obj.PlatformSubType = "NT";
							obj.Edition = "3.51";
							break;
						case 4:
							switch (osvi.wProductType)
							{
								case 1:
									obj.ClassName = "Windows.NT";
									obj.PlatformSubType = "NT";
									obj.Edition = "4.0 Workstation";
									break;
								case 3:
									if (osvi.wSuiteMask == SuiteVersion.Enterprise)
									{
										obj.ClassName = "Windows.NT";
										obj.PlatformSubType = "NT";
										obj.Edition = "4.0 Server Enterprise";
									}
									else
									{
										obj.ClassName = "Windows.NT";
										obj.PlatformSubType = "NT";
										obj.Edition = "4.0 Server Standard";
									}
									break;
							}
							break;
						case 5:
							switch (os.Version.Minor)
							{
								case 0:
									switch (osvi.wSuiteMask)
									{
										case SuiteVersion.DataCenter:
											obj.ClassName = "Windows.v2000";
											obj.PlatformSubType = "2000";
											obj.Edition = "Data Center";
											break;
										case SuiteVersion.Enterprise:
											obj.ClassName = "Windows.v2000";
											obj.PlatformSubType = "2000";
											obj.Edition = "Advanced";
											break;
										default:
											obj.ClassName = "Windows.v2000";
											obj.PlatformSubType = "2000";
											obj.Edition = "Standard";
											break;
									}
									break;
								case 1:
									if (osvi.wSuiteMask == SuiteVersion.Personal)
									{
										obj.ClassName = "Windows.XP";
										obj.PlatformSubType = "XP";
										obj.Edition = "Professional";
									}
									else
									{
										obj.ClassName = "Windows.XP";
										obj.PlatformSubType = "XP";
										obj.Edition = "Home";
									}
									break;
								case 2:
									if ((osvi.wProductType == NTVersion.Workstation) &&
									    (sysInfo.processorArchitecture == ProcessorArchitecture.AMD64))
									{
										obj.ClassName = "Windows.XP";
										obj.PlatformSubType = "XP";
										obj.Edition = "Professional x64";
									}
									else if ((osvi.wProductType == NTVersion.Server) &&
									         (GetSystemMetrics(SystemMetrics.ServerR2) == 0) &&
									         (osvi.wSuiteMask == SuiteVersion.Enterprise))
									{
										obj.ClassName = "Windows.v2003";
										obj.PlatformSubType = "Server";
										obj.Edition = "2003 Enterprise";
									}
									else if ((osvi.wProductType == NTVersion.Server) &&
									         GetSystemMetrics(SystemMetrics.ServerR2) != 0)
									{
										obj.ClassName = "Windows.v2003";
										obj.PlatformSubType = "Server";
										obj.Edition = "2003 R2";
									}
									else
									{
										switch (osvi.wSuiteMask)
										{
											case SuiteVersion.DataCenter:
												obj.ClassName = "Windows.v2003";
												obj.PlatformSubType = "Server";
												obj.Edition = "2003 Data Center";
												break;
											case SuiteVersion.Blade:
												obj.ClassName = "Windows.v2003";
												obj.PlatformSubType = "Server";
												obj.Edition = "2003 Web Edition";
												break;
											case SuiteVersion.WHServer:
												obj.ClassName = "Windows.v2003";
												obj.PlatformSubType = "2003";
												obj.Edition = "Home Server";
												break;
											default:
												obj.ClassName = "Windows.v2003";
												obj.PlatformSubType = "Server";
												obj.Edition = "2003 Standard";
												break;
										}
									}
									break;
							}
							break;
						case 6:
							Win32ProductInfo ospi = new Win32ProductInfo();
							ospi.dwOSProductInfoSize = Marshal.SizeOf(ospi);
							ospi.dwOSMajorVersion = os.Version.Major;
							ospi.dwOSMinorVersion = os.Version.Minor;
							ospi.dwSpMajorVersion = 0;
							ospi.dwSpMinorVersion = 0;
							
							GetProductInfo(ospi);
							obj.Version = obj.Version+"."+ospi.dwOSEdition;
							switch (os.Version.Minor)
							{
								case 0:
									if (osvi.wProductType == NTVersion.Workstation)
									{
										// Vista Detection
										switch (ospi.dwOSEdition)
										{
											case ProductType.Undefined:
												obj.ClassName = "Windows.Undefined";
												obj.PlatformSubType = "Vista";
												obj.Edition = "is not defined!";
												break;
											case ProductType.Ultimate: //    1
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Ultimate Edition";
												break;
											case ProductType.HomeBasic: // 2
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Home Basic Edition";
												break;
											case ProductType.HomePremium: // 3
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Home Premium Edition";
												break;
											case ProductType.Enterprise: // 4
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Enterprise Edition";
												break;
											case ProductType.HomeBasicN: // 5
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Home Basic N Edition (EU Only)";
												break;
											case ProductType.Business: // 6
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Business Edition";
												break;
											case ProductType.Starter:// B
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Starter Edition";
												break;
											case ProductType.BusinessN: // 10
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Business N Edition (EU Only)";
												break;
											case ProductType.HomePremiumN: // 1A
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Home Premium N Edition (EU Only)";
												break;
											case ProductType.EnterpriseN: // 1B
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Enterprise N Edition (EU Only)";
												break;
											case ProductType.UltimateN: // 1C
												obj.ClassName = "Windows.Vista";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Ultimate N Edition (EU Only)";
												break;
											case ProductType.Unlicensed: // 0xABCDABCD
												obj.ClassName = "Windows.Unlicensed";
												obj.PlatformSubType = "Vista";
												obj.Edition = "Unlicensed";
												break;
											default:
												obj.ClassName = "Windows.Unknown";
												obj.PlatformSubType = "Vista";
												obj.Edition = "is not defined!";
												break;
										}
									}
									else
									{
										switch (ospi.dwOSEdition)
										{
												//Windows 2008 Detection
												
											case ProductType.Undefined:
												obj.ClassName = "Windows.Undefined";
												obj.PlatformSubType = "2008";
												obj.Edition = "is not defined!";
												break;
											case ProductType.StandardServer: // 7
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Standard Server";
												break;
											case ProductType.DataCenterServer://8
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Data Center Server";
												break;
											case ProductType.SmallBusinessServer://9
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Small Business Server";
												break;
											case ProductType.EnterpriseServer:// A
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Enterprise Server";
												break;
											case ProductType.DataCenterServerCore: // C
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Data Center Server Core";
												break;
											case ProductType.StandardServerCore: // D
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Standard Server Core";
												break;
											case ProductType.EnterpriseServerCore: // E
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Enterprise Server Core";
												break;
											case ProductType.EnterpriseServerIA64: // F
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Enterprise Server IA64";
												break;
											case ProductType.WebServer: // 11
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Web Server";
												break;
											case ProductType.ClusterServer: // 12
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Cluster Server";
												break;
											case ProductType.HomeServer: // 13
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Home Server";
												break;
											case ProductType.StorageExpressServer: // 14
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Express Server";
												break;
											case ProductType.StorageStandardServer: // 15
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Standard Server";
												break;
											case ProductType.StorageWorkgroupServer: // 16
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Workgroup Server";
												break;
											case ProductType.StorageEnterpriseServer: // 17
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Enterprise Server";
												break;
											case ProductType.ServerForSmallBusiness: // 18
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Server for Small Businesses";
												break;
											case ProductType.SmallBusinessServerPremium: // 19
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Small Business Server Premium";
												break;
											case ProductType.WebServerCore: // 1D
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Web Server Core";
												break;
											case ProductType.MediumBusinessServerManagement: // 1E
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Medium Business Server Management";
												break;
											case ProductType.MediumBusinessServerSecurity: // 1F
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Medium Business Server Security";
												break;
											case ProductType.MediumBusinessServerMessaging: // 20
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Medium Business Server Messaging";
												break;
											case ProductType.SmallBusinessServerPrime: // 21
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Small Business Server Prime";
												break;
											case ProductType.HomePremiumServer: // 22
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Home Premium Server";
												break;
											case ProductType.ServerForSmallBusinessV: // 23
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Server for Small Business (Hyper-V)";
												break;
											case ProductType.StandardServerV: // 24
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Standard Server (Hyper-V)";
												break;
											case ProductType.DataCenterServerV: // 25
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Data Center Server (Hyper-V)";
												break;
											case ProductType.EnterpriseServerV: // 26
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Enterprise Server (Hyper-V)";
												break;
											case ProductType.DataCenterServerCoreV: // 27
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Data Center Server Core (Hyper-V)";
												break;
											case ProductType.StandardServerCoreV: // 28
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Standard Server Core (Hyper-V)";
												break;
											case ProductType.EnterpriseServerCoreV: // 29
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Enterprise Server Core (Hyper-V)";
												break;
											case ProductType.HyperV: // 2A
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "(Hyper-V)";
												break;
											case ProductType.StorageExpressServerCore: // 2B
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Express Server Core";
												break;
											case ProductType.StorageStandardServerCore: // 2C
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Standard Server Core";
												break;
											case ProductType.StorageWorkgroupServerCore: // 2D
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Workgroup Server Core";
												break;
											case ProductType.StorageEnterpriseServerCore: // 2E
												obj.ClassName = "Windows.v2008";
												obj.PlatformSubType = "2008";
												obj.Edition = "Storage Enterprise Server Core";
												break;
											case ProductType.Unlicensed: // 0xABCDABCD
												obj.ClassName = "Windows.Unlicensed";
												obj.PlatformSubType = "2008";
												obj.Edition = "Unlicensed";
												break;
											default:
												obj.ClassName = "Windows.Unknown";
												obj.PlatformSubType = "2008";
												obj.Edition = "is unknown!";
												break;
										}
									}
									break;
								case 1:
									switch (ospi.dwOSEdition)
									{
										case ProductType.Undefined:
											obj.ClassName = "Windows.Undefined";
											obj.PlatformSubType = "7";
											obj.Edition = "is undefined";
											break;
										case ProductType.Ultimate: //    1
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Ultimate Edition";
											break;
										case ProductType.HomeBasic: // 2
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Home Basic Edition";
											break;
										case ProductType.HomePremium: // 3
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Home Premium Edition";
											break;
										case ProductType.Enterprise: // 4
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Enterprise Edition";
											break;
										case ProductType.HomeBasicN: // 5
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Home Basic N Edition (EU only)";
											break;
										case ProductType.Business: // 6
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Business Edition";
											break;
										case ProductType.BusinessN: // 10
											obj.ClassName = "Windows.v7";
											obj.PlatformSubType = "7";
											obj.Edition = "Business N Edition (EU only)";
											break;
									}
									break;
							} //End os.Version.Minor
							break;
					} // End os.Version.Major
					break;
			} // End os.Platform
			
			if (obj.ClassName == null)
				throw new ArgumentNullException("ClassName was not defined. Please report this bug.");
			
			//Add project namespace
			obj.ClassName = "GitSharp.Platform."+obj.ClassName;
			
			return obj;
		}
	}

	
	
	//The following enums are located in WinNT.h
	internal static class NTVersion
	{
		public const byte Workstation         = 1;
		public const byte DomainController     = 2;
		public const byte Server             = 3;
	}

	// VER_SUITE_
	internal static class SuiteVersion
	{
		public const ushort Standard                    = 0x00000000;
		public const ushort SmallBusiness             = 0x00000001;
		public const ushort Enterprise                 = 0x00000002;
		public const ushort BackOffice                 = 0x00000004;
		public const ushort Communications             = 0x00000008;
		public const ushort Terminal                     = 0x00000010;
		public const ushort SmallBusinessRestricted     = 0x00000020;
		public const ushort EmbeddedNT                 = 0x00000040;
		public const ushort DataCenter                 = 0x00000080;
		public const ushort SingleUserTS                 = 0x00000100;
		public const ushort Personal                     = 0x00000200;
		public const ushort Blade                     = 0x00000400;
		public const ushort EmbeddedRestricted         = 0x00000800;
		public const ushort SecurityAppliance         = 0x00001000;
		public const ushort StorageServer             = 0x00002000;
		public const ushort ComputeServer             = 0x00004000;
		public const ushort WHServer                     = 0x00008000;
	}

	//PRODUCT_
	internal static class ProductType
	{
		public const uint Undefined                         = 0x00000000;
		public const uint Ultimate                         = 0x00000001;
		public const uint HomeBasic                         = 0x00000002;
		public const uint HomePremium                     = 0x00000003;
		public const uint Enterprise                         = 0x00000004;
		public const uint HomeBasicN                         = 0x00000005;
		public const uint Business                         = 0x00000006;
		public const uint StandardServer                     = 0x00000007;
		public const uint DataCenterServer                 = 0x00000008;
		public const uint SmallBusinessServer             = 0x00000009;
		public const uint EnterpriseServer                 = 0x0000000A;
		public const uint Starter                         = 0x0000000B;
		public const uint DataCenterServerCore             = 0x0000000C;
		public const uint StandardServerCore                 = 0x0000000D;
		public const uint EnterpriseServerCore             = 0x0000000E;
		public const uint EnterpriseServerIA64             = 0x0000000F;
		public const uint BusinessN                         = 0x00000010;
		public const uint WebServer                         = 0x00000011;
		public const uint ClusterServer                     = 0x00000012;
		public const uint HomeServer                         = 0x00000013;
		public const uint StorageExpressServer             = 0x00000014;
		public const uint StorageStandardServer             = 0x00000015;
		public const uint StorageWorkgroupServer             = 0x00000016;
		public const uint StorageEnterpriseServer         = 0x00000017;
		public const uint ServerForSmallBusiness             = 0x00000018;
		public const uint SmallBusinessServerPremium         = 0x00000019;
		public const uint HomePremiumN                     = 0x0000001A;
		public const uint EnterpriseN                     = 0x0000001B;
		public const uint UltimateN                         = 0x0000001C;
		public const uint WebServerCore                     = 0x0000001D;
		public const uint MediumBusinessServerManagement    = 0x0000001E;
		public const uint MediumBusinessServerSecurity     = 0x0000001F;
		public const uint MediumBusinessServerMessaging     = 0x00000020;
		public const uint SmallBusinessServerPrime         = 0x00000021;
		public const uint HomePremiumServer                 = 0x00000022;
		public const uint ServerForSmallBusinessV         = 0x00000023;
		public const uint StandardServerV                 = 0x00000024;
		public const uint DataCenterServerV                 = 0x00000025;
		public const uint EnterpriseServerV                 = 0x00000026;
		public const uint DataCenterServerCoreV             = 0x00000027;
		public const uint StandardServerCoreV             = 0x00000028;
		public const uint EnterpriseServerCoreV             = 0x00000029;
		public const uint HyperV                             = 0x0000002A;
		public const uint StorageExpressServerCore         = 0x0000002B;
		public const uint StorageStandardServerCore         = 0x0000002C;
		public const uint StorageWorkgroupServerCore         = 0x0000002D;
		public const uint StorageEnterpriseServerCore     = 0x0000002E;
		public const uint Unlicensed                        = 0xABCDABCD;
	}

	//PROCESSOR_ARCHITECTURE
	internal static class ProcessorArchitecture
	{
		public const ushort Intel         = 0;
		public const ushort IA64             = 6;
		public const ushort AMD64         = 9;
		public const ushort Unknown         = 0xFFFF;
	}

	internal static class SystemMetrics
	{
		///The build number if the system is Windows Server 2003 R2; otherwise, 0.
		public const int ServerR2 = 89;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal class SecurityAttributes
	{
    	public int nLength;
    	public IntPtr lpSecurityDescriptor;
    	public int bInheritHandle;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal class Win32ProductInfo
	{
		public int dwOSProductInfoSize;
    	public int dwOSMajorVersion;
    	public int dwOSMinorVersion;
    	public int dwSpMajorVersion;
    	public int dwSpMinorVersion;
    	public uint dwOSEdition;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct Win32SystemInfo
	{
    	public ushort processorArchitecture;
    	ushort reserved;
    	public uint pageSize;
    	public IntPtr minimumApplicationAddress;
    	public IntPtr maximumApplicationAddress;
    	public IntPtr activeProcessorMask;
    	public uint numberOfProcessors;
    	public uint processorType;
    	public uint allocationGranularity;
    	public ushort processorLevel;
    	public ushort processorRevision;
    }
	
	[StructLayout(LayoutKind.Sequential)]
	internal class Win32VersionInfo
	{
   		public int dwOSVersionInfoSize;
   		public int dwMajorVersion;
   		public int dwMinorVersion;
   		public int dwBuildNumber;
   		public int dwPlatformId;
   		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
   		public string szCSDVersion;
   		public short wServicePackMajor;  
   		public short wServicePackMinor;  
   		public ushort wSuiteMask;
   		public byte wProductType;  
   		public byte wReserved;
	}	
}