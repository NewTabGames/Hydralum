using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui
{
	internal class NotificationManager : MonoBehaviour
	{
		public readonly object lockObj = new object();
		public List<Notification> notifications = new List<Notification>();
		public bool DisableNotifications = false;

		public static Vector2 BoxSize
		{
			get { return new Vector2(325, 90) * MainUI.scale; }
		}

		public static Vector2 BoxHeaderSize
		{
			get { return new Vector2(BoxSize.x, 17 * MainUI.scale); }
		}

		public static Vector2 BoxContentPadding
		{
			get { return new Vector2(10, 0) * MainUI.scale; }
		}

		public static Vector2 BoxContentSize
		{
			get { return new Vector2(BoxSize.x - BoxContentPadding.x, BoxSize.y - BoxHeaderSize.y - BoxSliderSize.y); }
		}

		public static Vector2 BoxSliderSize
		{
			get { return new Vector2(BoxSize.x, 20 * MainUI.scale); }
		}

		public void Update()
		{
			try
			{
				lock (lockObj)
				{
					int maxNotifs = GetMaxNotifications();
					int notificationCount = Math.Min(maxNotifs, notifications.Count);

					for(int i = 0; i < notificationCount; i++)
					{
						if (i >= notifications.Count) break;
						Notification notification = notifications[i];
						notification.lifetime += Time.deltaTime;

						if(notification.HasExpired)
						{
							notifications.RemoveAt(i);
							i--;
							notificationCount--;
							continue;
						}
					}
				}
			}
			catch { }
		}

		public void OnGUI()
		{
			try
			{
				if(DisableNotifications) return;

				Notification[] snapshot;
				lock (lockObj)
				{
					snapshot = notifications.ToArray();
				}

				int maxNotifs = GetMaxNotifications();
				int notificationCount = Math.Min(maxNotifs, snapshot.Length);

				for(byte i = 0; i < notificationCount; i++)
				{
					if (i < snapshot.Length && snapshot[i] != null)
					{
						RenderNotification(i, snapshot[i]);
					}
				}
			}
			catch { }
		}

		[HideFromIl2Cpp]
		private void RenderNotification(byte position, Notification notification)
		{
			float boxX = Screen.width - BoxSize.x;
			float boxY = Screen.height - (int)(BoxSize.y * (position + 1));

			GUI.Box(new Rect(boxX, boxY, BoxSize.x, BoxSize.y), notification.title);

			GUI.Label(new Rect(boxX + BoxContentPadding.x, boxY + BoxHeaderSize.y, BoxContentSize.x, BoxContentSize.y), notification.message);

			GUI.HorizontalSlider(new Rect(boxX, boxY + BoxHeaderSize.y + BoxContentSize.y, BoxSize.x, BoxSize.y), notification.ttl - notification.lifetime, 0, notification.ttl);
		}

		public int GetMaxNotifications()
		{
			int boxH = Math.Max(1, (int)BoxSize.y);
			return Math.Max(1, Screen.height / 2 / boxH);
		}

		// The time to live value for a notification should be five seconds if it is a success message, and ten seconds if it is a failure message
		public void Send(string title, string message, float ttl = 10)
		{
			Hydra.Log?.LogMessage($"[Notification] [{title}] {message}");

			if(DisableNotifications) return;

			Notification notification = new Notification(title, message, ttl);
			lock (lockObj)
			{
				notifications.Add(notification);
			}
		}

		public void ClearNotifications()
		{
			lock (lockObj)
			{
				notifications.Clear();
			}
		}
	}
}