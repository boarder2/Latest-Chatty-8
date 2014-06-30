﻿using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Latest_Chatty_8.Shared.LayoutAwarePage, INotifyPropertyChanged
	{
		#region NPC
		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (object.Equals(storage, value)) return false;

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		#region Private Variables
		#endregion

		public bool IsLoading { get { return false; } }

		private CommentThread npcSelectedThread = null;
		public CommentThread SelectedThread
		{
			get { return this.npcSelectedThread; }
			set { this.SetProperty(ref this.npcSelectedThread, value); }
		}

		private ReadOnlyObservableCollection<CommentThread> npcCommentThreads;
		public ReadOnlyObservableCollection<CommentThread> CommentThreads
		{
			get { return this.npcCommentThreads; }
			set
			{
				this.SetProperty(ref this.npcCommentThreads, value);
			}
		}

		#region Constructor
		public Chatty()
		{
			this.InitializeComponent();
			this.chattyCommentList.AppBarToShow = this.bottomBar;
			//TODO: Figure out how to make this cross-platform
			//this.selectedThreadView.AppBarToShow = this.bottomBar;
			this.SizeChanged += Chatty_SizeChanged;
			this.chattyCommentList.SelectionChanged += ChattyListSelectionChanged;
			this.lastUpdateTime.DataContext = CoreServices.Instance;
		}

		void Chatty_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if(e.NewSize.Width < 800)
			{
				VisualStateManager.GoToState(this, "Vertical", true);
				this.chattyListGroup.MaxWidth = Double.PositiveInfinity;
				Grid.SetRow(this.chattyListGroup, 1);
				Grid.SetRowSpan(this.chattyListGroup, 1);
				Grid.SetColumn(this.chattyListGroup, 2);
				Grid.SetRow(this.divider, 2);
				Grid.SetRowSpan(this.divider, 1);
				Grid.SetColumn(this.divider, 2);
				this.divider.Width = Double.NaN;
				this.divider.Height = 7;
				this.lastUpdateTime.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				Grid.SetRow(this.selectedThreadView, 3);
				Grid.SetRowSpan(this.selectedThreadView, 1);
			}
			else
			{
				if(e.NewSize.Width < 900)
				{
					this.chattyListGroup.MaxWidth = 320;
				}
				else
				{
					this.chattyListGroup.MaxWidth = 400;
				}
				VisualStateManager.GoToState(this, "Default", true);
				Grid.SetRow(this.chattyListGroup, 1);
				Grid.SetRowSpan(this.chattyListGroup, 3);
				Grid.SetColumn(this.chattyListGroup, 0);
				Grid.SetRow(this.divider, 0);
				Grid.SetRowSpan(this.divider, 4);
				Grid.SetColumn(this.divider, 1);
				this.lastUpdateTime.Visibility = Windows.UI.Xaml.Visibility.Visible;
				this.divider.Width = 7;
				this.divider.Height = Double.NaN;
				Grid.SetRow(this.selectedThreadView, 0);
				Grid.SetRowSpan(this.selectedThreadView, 4);
			}
		}

		private void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Windows.UI.Xaml.Visibility vis = Windows.UI.Xaml.Visibility.Collapsed;
			try
			{
				if (e.AddedItems.Count > 0)
				{
					var comment = e.AddedItems[0] as CommentThread;
					comment.HasNewReplies = false;

					vis = Windows.UI.Xaml.Visibility.Visible;
				}
			}
			catch { }
			finally
			{
				this.threadAppBar.Visibility = vis;
				this.selectedThreadView.Visibility = vis;
			}
		}
		#endregion

		#region Load and Save State
		async protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			await CoreServices.Instance.ClearTile(true);
			this.CommentThreads = CoreServices.Instance.Chatty;
			this.sortThreadsButton.DataContext = CoreServices.Instance;
		}
		#endregion

		#region Overrides
		//async protected override void SettingsShown()
		//{
		//	 base.SettingsShown();
		//	 this.settingsVisible = true;
		//	 await this.ShowWebBrush();
		//}

		//protected override void SettingsDismissed()
		//{
		//	 base.SettingsDismissed();
		//	 this.settingsVisible = false;
		//	 this.ShowWebView();
		//}

		//async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		//{
		//	 base.CorePageKeyActivated(sender, args);
		//	 //If it's not a key down event, we don't care about it.
		//	 if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
		//			args.EventType == CoreAcceleratorKeyEventType.KeyDown)
		//	 {
		//		  var shiftDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		//		  var ctrlDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		//		  switch (args.VirtualKey)
		//		  {
		//				case Windows.System.VirtualKey.Z:
		//					 this.GoToNextComment(shiftDown);
		//					 break;

		//				case Windows.System.VirtualKey.A:
		//					 this.GoToPreviousComment(shiftDown);
		//					 break;

		//				case Windows.System.VirtualKey.P:
		//					 this.TogglePin();
		//					 break;

		//				case Windows.System.VirtualKey.F5:
		//					 if (ctrlDown)
		//					 {
		//						  this.chattyComments.Clear();
		//					 }
		//					 else
		//					 {
		//						  await this.GetSelectedThread();
		//					 }
		//					 break;

		//				case Windows.System.VirtualKey.Back:
		//					 this.Frame.GoBack();
		//					 break;
		//		  }
		//	 }
		//	 //Don't reply unless it's on keyup, to prevent the key up event from going to the reply page.
		//	 if (args.EventType == CoreAcceleratorKeyEventType.KeyUp)
		//	 {
		//		  if (args.VirtualKey == VirtualKey.R)
		//		  {
		//				await this.ReplyToThread();
		//		  }
		//	 }
		//	 return true;
		//}

		#endregion

		#region Events

		async private void MarkAllReadThread(object sender, RoutedEventArgs e)
		{
			if(this.SelectedThread != null)
			{
				await CoreServices.Instance.MarkCommentThreadRead(this.SelectedThread);
			}
		}

		async private void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await CoreServices.Instance.MarkAllCommentsRead();
		}

		async private void PinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.PinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}

		async private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.UnPinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}

		async private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			await this.ReplyToThread();
		}

		async private void RefreshChattyClicked(object sender, RoutedEventArgs e)
		{
			int selectedId = -1;
			if (this.chattyCommentList.SelectedItem != null)
			{
				var thread = this.chattyCommentList.SelectedItem as CommentThread;
				if (thread != null)
				{
					selectedId = thread.Id;
				}
			}

			await CoreServices.Instance.RefreshChatty();
			var focusedComment = CoreServices.Instance.Chatty.FirstOrDefault(c => c.Id == selectedId);
			if (focusedComment != null)
			{
				var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
				 {
					 this.chattyCommentList.SelectedItem = focusedComment;
					 this.chattyCommentList.ScrollIntoView(focusedComment);
				 });
			}
		}

		private void NewRootPostClicked(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(ReplyToCommentView));
		}
		#endregion

		#region Private Helpers

		async private Task ReplyToThread()
		{
			if (!CoreServices.Instance.LoggedIn)
			{
				var dialog = new MessageDialog("You must login before you can post.  Login information can be set in the application settings.");
				await dialog.ShowAsync();
				return;
			}

			var comment = this.selectedThreadView.SelectedComment as Comment;
			if (comment != null && this.SelectedThread != null)
			{
				this.Frame.Navigate(typeof(ReplyToCommentView), new ReplyNavParameter(comment, this.SelectedThread));
			}
		}

		#endregion

		async private void lolPostClicked(object sender, RoutedEventArgs e)
		{
			this.tagButton.IsEnabled = false;
			try
			{
				var comment = this.selectedThreadView.SelectedComment as Comment;
				if (comment != null && this.SelectedThread != null)
				{
					var mi = sender as MenuFlyoutItem;
					var tag = mi.Text;
					await comment.LolTag(tag);
				}
			}
			finally
			{
				this.tagButton.IsEnabled = true;
			}
		}

		private void ReSortClicked(object sender, RoutedEventArgs e)
		{
			CoreServices.Instance.CleanupChattyList();
			this.chattyCommentList.ScrollIntoView(CoreServices.Instance.Chatty.First(c => !c.IsPinned), ScrollIntoViewAlignment.Leading);
		}

		
	}
}
