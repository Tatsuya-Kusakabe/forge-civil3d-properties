/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

namespace AspNetCore.Controllers
{
	public static class Utils
	{

		public static string NickName
		{
			get { return Credentials.GetAppSetting("FORGE_CLIENT_ID"); }
		}

		public static string BucketName
		{
			get { return NickName.ToLower() + "-designatomaiton_civil3d"; }
		}

		private static readonly char[] padding = { '=' };

		/// <summary>
		/// Base64 encode a string (source: http://stackoverflow.com/a/11743162)
		/// </summary>
		/// <param name="plainText"></param>
		/// <returns></returns>
		public static string Base64Encode(this string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
		}

		/// <summary>
		/// Base64 dencode a string (source: http://stackoverflow.com/a/11743162)
		/// </summary>
		/// <param name="base64EncodedData"></param>
		/// <returns></returns>
		public static string Base64Decode(this string base64EncodedData)
		{
			string incoming = base64EncodedData.Replace('_', '/').Replace('-', '+');
			switch (base64EncodedData.Length % 4)
			{
				case 2: incoming += "=="; break;
				case 3: incoming += "="; break;
			}
			var base64EncodedBytes = System.Convert.FromBase64String(incoming);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}
	}
}
