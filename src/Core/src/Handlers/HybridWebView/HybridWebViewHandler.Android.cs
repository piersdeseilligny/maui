﻿using System;
using Android.Webkit;
using Java.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Android.Views.ViewGroup;
using AWebView = Android.Webkit.WebView;

namespace Microsoft.Maui.Handlers
{
	public partial class HybridWebViewHandler : ViewHandler<IHybridWebView, AWebView>
	{
		// This name matches the name of the API used in HybridWebView.js and must remain in sync
		private const string HybridWebViewHostJsName = "hybridWebViewHost";

		private HybridWebViewJavaScriptInterface? _javaScriptInterface;

		protected override AWebView CreatePlatformView()
		{
			var platformView = new MauiHybridWebView(this, Context!)
			{
				LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
			};

			platformView.Settings.DomStorageEnabled = true;
			platformView.Settings.SetSupportMultipleWindows(true);

			// Note that this is a per-app setting and not per-control, so if you enable
			// this, it is enabled for all Android WebViews in the app.
			AWebView.SetWebContentsDebuggingEnabled(enabled: true); // TODO: Get from setting

			platformView.Settings.JavaScriptEnabled = true;

			_javaScriptInterface = new HybridWebViewJavaScriptInterface(this);
			platformView.AddJavascriptInterface(_javaScriptInterface, HybridWebViewHostJsName);

			return platformView;
		}

		private sealed class HybridWebViewJavaScriptInterface : Java.Lang.Object
		{
			private readonly WeakReference<HybridWebViewHandler> _hybridWebViewHandler;

			public HybridWebViewJavaScriptInterface(HybridWebViewHandler hybridWebViewHandler)
			{
				_hybridWebViewHandler = new(hybridWebViewHandler);
			}

			private HybridWebViewHandler? Handler => _hybridWebViewHandler is not null && _hybridWebViewHandler.TryGetTarget(out var h) ? h : null;

			[JavascriptInterface]
			[Export("sendRawMessage")]
			public void SendRawMessage(string message)
			{
				Handler?.VirtualView?.RawMessageReceived(message);
			}
		}

		protected override void ConnectHandler(AWebView platformView)
		{
			base.ConnectHandler(platformView);

			var webViewClient = new MauiHybridWebViewClient(this);
			PlatformView.SetWebViewClient(webViewClient);

			platformView.LoadUrl(new Uri(AppOriginUri, "/").ToString());
		}

		protected override void DisconnectHandler(AWebView platformView)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(26))
			{
				if (platformView.WebViewClient is MauiHybridWebViewClient webViewClient)
				{
					webViewClient.Disconnect();
				}
				//if (platformView.WebChromeClient is MauiWebChromeClient webChromeClient)
				//{
				//	webChromeClient.Disconnect();
				//}
			}

			platformView.SetWebViewClient(null!);
			//platformView.SetWebChromeClient(null);

			platformView.StopLoading();


			base.DisconnectHandler(platformView);
		}

		public static void MapSendRawMessage(IHybridWebViewHandler handler, IHybridWebView hybridWebView, object? arg)
		{
			if (arg is not string rawMessage || handler.PlatformView is not IHybridPlatformWebView hybridPlatformWebView)
			{
				return;
			}

			hybridPlatformWebView.SendRawMessage(rawMessage);
		}
	}
}
