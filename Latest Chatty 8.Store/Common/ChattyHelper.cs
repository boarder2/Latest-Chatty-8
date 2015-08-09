﻿using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public static class ChattyHelper
	{
		async public static Task<bool> ReplyToComment(this Comment commentToReplyTo, string content, AuthenticationManager authenticationManager)
		{
			return await ChattyHelper.PostComment(content, authenticationManager, commentToReplyTo.Id.ToString());
		}

		async public static Task<bool> PostRootComment(string content, AuthenticationManager authenticationManager)
		{
			return await ChattyHelper.PostComment(content, authenticationManager);
		}

		async private static Task<bool> PostComment(string content, AuthenticationManager authenticationManager, string parentId = null)
		{
			if (content.Length <= 5)
			{
				var dlg = new Windows.UI.Popups.MessageDialog("Post something longer.");
				await dlg.ShowAsync();
				return false;
			}

			var data = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("text", content),
				new KeyValuePair<string, string>("parentId", parentId != null ? parentId : "0")
			};

			var response = await POSTHelper.Send(Locations.PostUrl, data, true, authenticationManager);

			return true;
		}
	}
}
