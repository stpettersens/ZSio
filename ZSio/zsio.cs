/*
	ZSio: ZS data format library for C#.
	Copyright (c) 2018 Sam Saint-Pettersen.
*/
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using DamienG.Security.Cryptography;

namespace ZSio
{
	class ZSio
	{
		/* Magic numbers */
		private static byte[] m_zsBadMagic = new byte[]
		{
			0xab, 0x5a, 0x53, 0x74, 0x6f, 0x42, 0x65, 0x01 // "ZStoBe"
		};
		
		private static byte[] m_zsGoodMagic = new byte[]
		{
			0xab, 0x5a, 0x53, 0x66, 0x69, 0x4c, 0x65, 0x01 // "ZSfiLe"
		};
		
		/* SHA-256 Hashing */
		private static byte[] GetSha256Bytes(string data)
		{
			var crypt = new SHA256Managed();
			return crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
		}
		
		/* CRC */
		private static byte[] CalculateCRC(string chksmFile)
		{
			List<byte> checksum = new List<byte>();
			var crc64 = new Crc64Iso();
			using (FileStream fs = File.Open(chksmFile, FileMode.Open))
			{
				foreach (byte b in crc64.ComputeHash(fs))
				{
					checksum.Add(b);
				}
			}
			
			return checksum.ToArray();
		}
		
		/* Get ULEB128 value */
		private static byte[] GetUleb128(uint Value)
		{
			// Adapted from: https://github.com/jonathanvdc/binary-loyc-tree
			List<byte> uleb128 = new List<byte>();
			do
			{
				byte b = (byte)(Value & 0x7F);
				Value >>= 7;
				if (Value != 0) /* More bytes to come... */
					b |= 0x80;
					
				uleb128.Add(b);
			}
			while (Value != 0);
				
			return uleb128.ToArray();
		}
		
		private static string m_time = DateTime.UtcNow.ToString
		("yyyy-MM-ddTHH:mm:ss.ffffffZ");
		
		/* Host-user-format-time metadata */
		private static string[] m_metadata = new string[]
		{
			"{\"build-info\": {",
			$"\"host\": \"{Environment.MachineName}\", ",
			$"\"version: \"zs 0.10.0\", \"user\": \"{Environment.UserName}\", ",
			$"\"time\": \"{m_time}\"}}}}"
		};
		
		public static void MakeZSFile(string filename, string data)
		{
			string codec = "none".PadRight(16, '\0');
			string f_metadata = string.Concat(m_metadata);
			
			/* Magic number and header */
			using (BinaryWriter zs = new BinaryWriter(File.Open(filename, FileMode.Create)))
			{
				zs.Write(m_zsGoodMagic); // Magic number. Should use bad magic here, but for now use good magic.
				zs.Write(8 + 0 + 0 + 224 + // Header length.
				GetSha256Bytes(data).Length + 
				codec.Length + 
				f_metadata.Length
				);
				zs.Write(0); // Root offset.
				zs.Write(0); // Root length.
				zs.Write(224); // File length.
				zs.Write(GetSha256Bytes(data)); // SHA-256 of data.
				zs.Write(codec); // Codec.
				zs.Write(f_metadata.Length); // Metadata length.
				zs.Write(f_metadata); // Metadata.
			}
			
			using (BinaryWriter cf = new BinaryWriter(File.Open("zsh.chksm", FileMode.Create)))
			{
				cf.Write(0); // Root offset.
				cf.Write(0); // Root length.
				cf.Write(0); // File length.
				cf.Write(GetSha256Bytes(data)); // SHA-256 of data.
				cf.Write(codec); // Codec.
				cf.Write(f_metadata.Length); // Metadata length.
				cf.Write(f_metadata); // Metadata.
			}
			
			byte[] chksm = CalculateCRC("zsh.chksm");
			
			/* Data block */
			using (BinaryWriter zs = new BinaryWriter(File.Open(filename, FileMode.Append)))
			{
				zs.Write(chksm); // Check sum for header.
				zs.Write(GetUleb128((uint)data.Length + 2)); // Length of data in block.
				zs.Write((byte)0); // Level.
				zs.Write(GetUleb128((uint)data.Length));
				zs.Write(data); // Data.
			}
			
			using (BinaryWriter cf = new BinaryWriter(File.Open("zsd.chksm", FileMode.Create)))
			{
				cf.Write((byte)0); // Level.
				cf.Write(data); // Data.
			}
			
			chksm = CalculateCRC("zsd.chksm");
			
			/*using (BinaryWriter db = new BinaryWriter(File.Open("zsd.data", FileMode.Create)))
			{
				db.Write(GetUleb128((uint)1 + data.Length)); // 
				db.Write((byte)0); // Level;
			}*/
			
			/* Checksum for data and then Index block */
			using (BinaryWriter zs = new BinaryWriter(File.Open(filename, FileMode.Append)))
			{
				zs.Write(chksm);
				// !TODO
			}
		}
	}
}
