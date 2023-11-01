using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrhoRunner = System.Func<System.Type, Urho.ApplicationOptions, System.Threading.Tasks.Task<Urho.Application>>;

namespace Urho_Test {
    //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/handlers/
    //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/handlers/create
    public interface IUrhoSurface : IView {
        //https://github.com/dotnet/maui/wiki/Porting-Custom-Renderers-to-Handlers/53ff0fcdb9f6dcb6657da3b504300f792da3742e
        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/handlers/create#create-the-handler
    }

    public class UrhoSurface : Microsoft.Maui.Controls.View, IUrhoSurface {
        //https://github.com/dotnet/maui/wiki/Porting-Custom-Renderers-to-Handlers/53ff0fcdb9f6dcb6657da3b504300f792da3742e

        //The control should provide a public API that will be accessed by its handler, and control consumers.Cross-platform controls should derive from View, which represents a visual element that's used to place layouts and views on the screen.

        //private readonly TaskCompletionSource<Func<Type, Urho.ApplicationOptions, Task<Urho.Application>>> runnerInitTaskSource = new TaskCompletionSource<Func<Type, Urho.ApplicationOptions, Task<Urho.Application>>>();
        readonly TaskCompletionSource<UrhoRunner> runnerInitTaskSource = new TaskCompletionSource<UrhoRunner>();

        internal void RegisterRunner(Func<Type, Urho.ApplicationOptions, Task<Urho.Application>> runner) {
            Debug.WriteLine("UrhoSurface | REGISTER RUNNER"); //NOT RUNNING ==need this to get application
            runnerInitTaskSource.TrySetResult(runner);
        }

        //https://github.com/xamarin/urho/blob/master/Bindings/Forms/UrhoSurface.cs
        public async Task<TUrhoApplication> Show<TUrhoApplication>(Urho.ApplicationOptions options) where TUrhoApplication : Urho.Application {
            Debug.WriteLine("UrhoSurface | SHOW"); //this is being run

            //===================THIS TASK IS NOT FINISHING====================================================

            var runner = await runnerInitTaskSource.Task;
            Debug.WriteLine("GOT RUNNER RETURNED");
            if (runner == null) {
                throw new InvalidOperationException("UrhoRunner should not be null.");
            }
            var appReturn = await runner(typeof(TUrhoApplication), options);
            Debug.WriteLine("GOT APP RETURNED");
            return (TUrhoApplication)appReturn;
        }

        public static void OnPause() {
            throw new InvalidOperationException("Platform implementation is not referenced");
        }

        public static void OnResume() {
            throw new InvalidOperationException("Platform implementation is not referenced");
        }

        public static void OnDestroy() {
            throw new InvalidOperationException("Platform implementation is not referenced");
        }
    }

#if WINDOWS
    public partial class UrhoSurfacePlatformView : Microsoft.UI.Xaml.Controls.SwapChainPanel {
    //public partial class MauiUrhoSurface : Microsoft.UI.Xaml.Controls.CalendarView {
        //https://github.com/xamarin/urho/blob/master/Bindings/UWP/UrhoSurface.cs
        static bool paused;
        bool stop;
        bool inited;
        bool firstFrameRendered;
        TaskCompletionSource<bool> loadedTaskSource;
        Urho.Application activeApp;
        UrhoSurface urhoSurface;
        SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public UrhoSurfacePlatformView(UrhoSurface urhoSurface) {
            Debug.WriteLine("UrhoSurfacePlatformView  | MAUI URHO SURFACE CONSTRUCTOR RUN");
            this.urhoSurface = urhoSurface; //SAVE IT FOR SOMETHING??? CHECK IT FOR CHAGNES ETC???

            //Opacity = 0;
            loadedTaskSource = new TaskCompletionSource<bool>();
            urhoSurface.RegisterRunner(UrhoLauncher);
            //Loaded += (s, e) => loadedTaskSource.TrySetResult(true);
            Debug.WriteLine("UrhoSurfacePlatformView | SUBSCRIBE LOADED");
            Loaded += UrhoSurface_Loaded;
            Unloaded += UrhoSurface_Unloaded;
            SizeChanged += UrhoSurface_SizeChanged;
        }
        void UrhoSurface_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Debug.WriteLine("UrhoSurfacePlatformView  | LOADED URHO SURFACE"); //run okay
            bool result = loadedTaskSource.TrySetResult(true);
            Debug.WriteLine("UrhoSurfacePlatformView  | LOADED SUCESSFUL: " + result);//run okay

        }
        async Task<Urho.Application> UrhoLauncher(Type type, Urho.ApplicationOptions opts) {
            try {
                //https://github.com/xamarin/urho/blob/cfeff3d45eaaee536e978f857c453dde3ec1c7ed/Bindings/Forms.UWP/UwpSurfaceRenderer.cs
                await semaphore.WaitAsync();
                //var urhoSurface = await surfaceTask.Task;
                await this.WaitLoadedAsync();
                return this.Run(type, opts);
            }
            finally {
                semaphore.Release();
            }
        }
        public Task WaitLoadedAsync() => loadedTaskSource.Task;

        void UrhoSurface_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e) {
            Debug.WriteLine("UrhoSurfacePlatformView  | URHO SURFACE SIZE CHANGED"); //run okay
            if (!inited)
                return;

            Urho.Sdl.SendWindowEvent(Urho.SdlWindowEvent.SDL_WINDOWEVENT_RESIZED, (int)ActualWidth, (int)ActualHeight);
        }


        void UrhoSurface_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
            Debug.WriteLine("UrhoSurfacePlatformView  | URHO SURFACE UNLOADED");
            if (Urho.Application.HasCurrent && Urho.Application.Current == activeApp) {
                //activeApp.StopCurrent().Wait();
                //Urho.Application.Current.StopCurrent().Wait();
                //Urho.Application.StopCurrent().Wait();
            }
        }


        public TGame Run<TGame>(Urho.ApplicationOptions options = null) where TGame : Urho.Application {
            Debug.WriteLine("UrhoSurfacePlatformView | T GAME RUN FUNCTION"); //NOT RUN

            return (TGame)Run(typeof(TGame), options);
        }

        public Urho.Application Run(Type appType, Urho.ApplicationOptions options = null) {
            Debug.WriteLine("UrhoSurfacePlatformView | RUN FUNCTION"); //NOT RUN

            Opacity = 0;
            //Urho.Application.Current.StopCurrent().Wait();
            //activeApp.StopCurrent().Wait();
            //Urho.Application.StopCurrent().Wait();
            options = options ?? new Urho.ApplicationOptions();
            options.Width = (int)ActualWidth;
            options.Height = (int)ActualHeight;
            stop = false;
            paused = false;
            inited = false;
            firstFrameRendered = false;
            Debug.WriteLine("about to init uwp sdl" + MainThread.IsMainThread);
            Urho.Sdl.InitUwp(); //protected memory error
            var app = activeApp = Urho.Application.CreateInstance(appType, options);
            app.Run();
            inited = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                await Task.Delay(16);//skip first frame
                while (stopRenderingTask == null) {
                    if (!paused && !app.IsExiting) {
                        app.Engine.RunFrame();
                        if (!firstFrameRendered) {
                            firstFrameRendered = true;
                            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => Opacity = 1);
                        }
                    }
                }
                stopRenderingTask.TrySetResult(true);
                stopRenderingTask = null;
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return app;
        }

        internal static TaskCompletionSource<bool> stopRenderingTask;
        internal static Task StopRendering() {
            stopRenderingTask = new TaskCompletionSource<bool>();
            return stopRenderingTask.Task;
        }

        public static void Pause() {
            paused = true;
        }

        public static void Resume() {
            paused = false;
        }
    }
#elif ANDROID
    public partial class UrhoSurfacePlatformView : Android.Widget.FrameLayout {
        //https://github.com/xamarin/urho/blob/master/Bindings/Android/UrhoSurface.cs
        //https://github.com/xamarin/urho/blob/master/Bindings/Forms.Droid/AndroidSurfaceRenderer.cs
        public UrhoSurfacePlatformView(Android.Content.Context context) : base(context) {
            Android.Widget.FrameLayout surfaceViewPlaceholder;
            AddView(surfaceViewPlaceholder = new Android.Widget.FrameLayout(Context));
        }
    }
#elif IOS
    public partial class UrhoSurfacePlatformView : UIKit.UIView {

        //https://github.com/xamarin/urho/blob/master/Bindings/Forms.iOS/IosSurfaceRenderer.cs

        static readonly SemaphoreSlim launcherSemaphore = new SemaphoreSlim(1);
		static UrhoSurface surface;
		static Urho.Application app;

		internal async Task<Urho.Application> Launcher(Type type, ApplicationOptions options)
		{
			await launcherSemaphore.WaitAsync();
			if (surface != null)
			{
				await surface.Stop();
				surface.RemoveFromSuperview ();
			}
			surface = new Urho.iOS.UrhoSurface(this.Bounds);
			surface.AutoresizingMask = UIViewAutoresizing.All;
			this.Add(surface);
			app = await surface.Show(type, options);
			launcherSemaphore.Release();
			return app;
		}

    }
#else
    public partial class UrhoSurfacePlatformView : Object {


}

#endif
#if WINDOWS
    public partial class UrhoSurfaceHandler : Microsoft.Maui.Handlers.ViewHandler<UrhoSurface, UrhoSurfacePlatformView> {
        public static IPropertyMapper<UrhoSurface, UrhoSurfaceHandler> PropertyMapper = new PropertyMapper<UrhoSurface, UrhoSurfaceHandler>(UrhoSurfaceHandler.ViewMapper) {

            [nameof(IUrhoSurface.Background)] = MyMapBackground, //not sure if need both
            [nameof(UrhoSurface.Background)] = MyMapBackground, //not sure if need both

            //[nameof(Video.AreTransportControlsEnabled)] = MapAreTransportControlsEnabled,
            //[nameof(Video.Source)] = MapSource,
            //[nameof(Video.IsLooping)] = MapIsLooping,
            //[nameof(Video.Position)] = MapPosition
        };

        private static void MyMapBackground(UrhoSurfaceHandler handler, UrhoSurface surface) {
            Debug.WriteLine("MAP BACKGROUND");
            //throw new NotImplementedException();
        }

        public UrhoSurfaceHandler() : base(PropertyMapper) {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CREATED");
        }
        
        protected override UrhoSurfacePlatformView CreatePlatformView() {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CREATES PLATFORM VIEW");
            //where you do per platform set up 
            return new UrhoSurfacePlatformView(VirtualView);

        }
        protected override void ConnectHandler(UrhoSurfacePlatformView platformView) {
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER TRYING TO CONNECT");
            base.ConnectHandler(platformView);
            Debug.WriteLine("UrhoSurfaceHandler | HANDLER CONNECTED");
            Debug.WriteLineIf(platformView != null, "UrhoSurfaceHandler | platform type: " + platformView.GetType());

            // Perform any control setup here
        }
        
        protected override void DisconnectHandler(UrhoSurfacePlatformView platformView) {
            //platformView.Dispose();
            Debug.WriteLine("UrhoSurfaceHandler | DISCONNECT HANDLER");
            base.DisconnectHandler(platformView);
        }
    }
#elif ANDROID
    public partial class UrhoSurfaceHandler : Microsoft.Maui.Handlers.ViewHandler<UrhoSurface, UrhoSurfacePlatformView> {
    }
#elif IOS
public partial class UrhoSurfaceHandler : Microsoft.Maui.Handlers.ViewHandler<UrhoSurface, UrhoSurfacePlatformView> {
    }
#else
#endif

  
}

//NEED TO ADD THESE:
public static class UrhoApplicationExtensions {
    /*public static async Task StopCurrent(this Urho.Application app) {
        if (app.current == null || !current.IsActive)
            return;

#if __ANDROID__
			current.WaitFrameEnd();
			Org.Libsdl.App.SDLActivity.OnDestroy();
			return;
#endif
        Current.Input.Enabled = false;
        isExiting = true;
#if __IOS__
			UrhoSurface.StopRendering(current);
#endif

#if WINDOWS_UWP && !UWP_HOLO
			UWP.UrhoSurface.StopRendering().Wait();
#endif
        LogSharp.Debug($"StopCurrent: Current.IsFrameRendering={Current.IsFrameRendering}");
        if (Current.IsFrameRendering)// && !Current.Engine.PauseMinimized)
        {
            waitFrameEndTaskSource = new TaskCompletionSource<bool>();
            await waitFrameEndTaskSource.Task;
            LogSharp.Debug($"StopCurrent: waitFrameEndTaskSource awaited");
            waitFrameEndTaskSource = null;
        }
        LogSharp.Debug($"StopCurrent: Engine.Exit");

        Current.Engine.Exit();

#if NET46
			if (Current.Options.DelayedStart)
#endif
        ProxyStop(Current.Handle);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }*/
}
//https://github.com/xamarin/urho/blob/cfeff3d45eaaee536e978f857c453dde3ec1c7ed/Bindings/Portable/Application.cs#L243
