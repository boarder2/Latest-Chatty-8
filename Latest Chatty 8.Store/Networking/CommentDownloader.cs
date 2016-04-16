﻿using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	/// <summary>
	/// Comment downloading helper methods
	/// </summary>
	public static class CommentDownloader
	{
		public async static Task<CommentThread> TryDownloadThreadById(int threadId, SeenPostsManager seenPostsManager, AuthenticationManager authManager, LatestChattySettings settings, ThreadMarkManager markManager, UserFlairManager flairManager, IgnoreManager ignoreManager)
		{
			var threadJson = await JSONDownloader.Download($"{Locations.GetThread}?id={threadId}");
			var threads = await ParseThreads(threadJson, seenPostsManager, authManager, settings, markManager, flairManager, ignoreManager);
			return threads.FirstOrDefault();
		}

		public async static Task<List<CommentThread>> ParseThreads(JToken chatty, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, UserFlairManager flairManager, IgnoreManager ignoreManager)
		{
			if (chatty == null) return null;
			var threadCount = chatty["threads"].Count();
			var parsedChatty = new CommentThread[threadCount];
			await Task.Run(() =>
			{
				Parallel.For(0, threadCount, (i) =>
				{
					var thread = chatty["threads"][i];
					var t = TryParseThread(thread, 0, seenPostsManager, services, settings, markManager, flairManager, ignoreManager);
					t.Wait();
					parsedChatty[i] = t.Result;
				});
			});
			
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				foreach (var thread in parsedChatty)
				{
					if (thread == null) continue;
					thread.RecalculateDepthIndicators();
				}
			});

#if DEBUG
			TreeImageRepo.PrintDebugInfo();
#endif

			var list = parsedChatty.Where(t => t != null).ToList();
#if GENERATE_THREADS
			if (System.Diagnostics.Debugger.IsAttached)
			{
				list.Add(ChattyHelper.GenerateMassiveThread(services, settings));
			}
#endif
			return list;
		}

		#region Private Helpers
		public async static Task<CommentThread> TryParseThread(JToken jsonThread, int depth, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, UserFlairManager flairManager, IgnoreManager ignoreManager, string originalAuthor = null, bool storeCount = true)
		{
			var threadPosts = jsonThread["posts"];

			var firstJsonComment = threadPosts.First(j => j["id"].ToString().Equals(jsonThread["threadId"].ToString()));

			var rootComment = await TryParseCommentFromJson(firstJsonComment, null, seenPostsManager, services, flairManager, ignoreManager); //Get the first comment, this is what we'll add everything else to.

			if (rootComment == null) return null;

			var thread = new CommentThread(rootComment, settings);
			var markType = markManager.GetMarkType(thread.Id);
			if (markType == MarkType.Unmarked)
			{
				//If it's not marked, find out if it should be collapsed because of auto-collapse.
				if (settings.ShouldAutoCollapseCommentThread(thread))
				{
					await markManager.MarkThread(thread.Id, MarkType.Collapsed, true);
					thread.IsCollapsed = true;
				}
			}
			else
			{
				thread.IsPinned = markType == MarkType.Pinned;
				thread.IsCollapsed = markType == MarkType.Collapsed;
			}

			await RecursiveAddComments(thread, rootComment, threadPosts, seenPostsManager, services, flairManager, ignoreManager);
			thread.HasNewReplies = thread.Comments.Any(c => c.IsNew);

			return thread;
		}

		private async static Task RecursiveAddComments(CommentThread thread, Comment parent, JToken threadPosts, SeenPostsManager seenPostsManager, AuthenticationManager services, UserFlairManager flairManager, IgnoreManager ignoreManager)
		{
			thread.AddReply(parent, false);
			var childPosts = threadPosts.Where(c => c["parentId"].ToString().Equals(parent.Id.ToString()));

			if (childPosts != null)
			{
				foreach (var reply in childPosts)
				{
					var c = await TryParseCommentFromJson(reply, parent, seenPostsManager, services, flairManager, ignoreManager);
					if (c != null)
					{
						await RecursiveAddComments(thread, c, threadPosts, seenPostsManager, services, flairManager, ignoreManager);
					}
				}
			}

		}

		public async static Task<Comment> TryParseCommentFromJson(JToken jComment, Comment parent, SeenPostsManager seenPostsManager, AuthenticationManager services, UserFlairManager flairManager, IgnoreManager ignoreManager)
		{
			var commentId = (int)jComment["id"];
			var parentId = (int)jComment["parentId"];
			var category = (PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jComment["category"], "ontopic"));
			var author = ParseJTokenToDefaultString(jComment["author"], string.Empty);
			var date = jComment["date"].ToString();
			var body = System.Net.WebUtility.HtmlDecode(ParseJTokenToDefaultString(jComment["body"], string.Empty).Replace("<a target=\"_blank\" rel=\"nofollow\"", " <a target=\"_blank\""));
			var preview = HtmlRemoval.StripTagsRegexCompiled(body.Replace("\r<br />", " ").Replace("<br />", " ").Replace(char.ConvertFromUtf32(8232), " ")); //8232 is Unicode LINE SEPARATOR.  Saw this occur in post ID 34112371.
			preview = preview.Substring(0, Math.Min(preview.Length, 300));
			var isTenYearUser = await flairManager.IsTenYearUser(author);
			var c = new Comment(commentId, category, author, date, preview, body, parent != null ? parent.Depth + 1 : 0, parentId, isTenYearUser, services, seenPostsManager);
			if (await ignoreManager.ShouldIgnoreComment(c)) return null;

			foreach (var lol in jComment["lols"])
			{
				var count = (int)lol["count"];
				switch (lol["tag"].ToString())
				{
					case "lol":
						c.LolCount = count;
						break;
					case "inf":
						c.InfCount = count;
						break;
					case "unf":
						c.UnfCount = count;
						break;
					case "tag":
						c.TagCount = count;
						break;
					case "wtf":
						c.WtfCount = count;
						break;
					case "ugh":
						c.UghCount = count;
						break;
				}
			}
			return c;
		}

		private static string ParseJTokenToDefaultString(JToken token, string defaultString)
		{
			var stringVal = (string)token;

			if (String.IsNullOrWhiteSpace(stringVal) || stringVal.Equals("null"))
			{
				stringVal = defaultString;
			}

			return stringVal;
		}
		#endregion
	}
}
