using Microsoft.Maui.Platform;
using System.Diagnostics;

namespace Urho_Test {
    public partial class App : Application {
        UrhoSurface helloWorldUrhoSurface;

        public App() {
            InitializeComponent();

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) {
                System.Diagnostics.Debug.WriteLine($"*** UNHANDLED EXCEPTION! Details: {e.Exception.ToString()}  {e.ToString()}" );
            }
            
            ContentPage mainPage = new ContentPage();
            this.MainPage = mainPage;
            
            Urho.Sdl.Quit(); //just for test purposes, not actually needed here

            AbsoluteLayout abs1 = new();
            AbsoluteLayout abs2 = new();
            mainPage.Content = abs1;
            abs1.Add(abs2);
            Debug.WriteLine("ABOUT TO CREATE URHO SURFACE");
            helloWorldUrhoSurface = new();
            mainPage.HandlerChanged += delegate {
                Debug.WriteLine("ASSIGN HANDLER");
                if (mainPage.Handler.MauiContext != null) {
                    //POST BUG IT IS BROKEN - NEEDS THIS TO RUN AND NO EXPOSED METHOD TO GET MAUI CONTEXT
                    //https://stackoverflow.com/questions/73382692/custom-maui-handler-control-creates-handler-when-used-in-xaml-but-when-created-w

                    //https://github.com/microsoft/microsoft-ui-xaml/issues/1806
                    helloWorldUrhoSurface.ToHandler(mainPage.Handler.MauiContext);

                    abs2.Add(helloWorldUrhoSurface); //THIS BREAKS IT

                }
            };
            Debug.WriteLine("CREATE URHO SURFACE");
            //abs2.Add(helloWorldUrhoSurface); //THIS BREAKS IT
            Debug.WriteLine("CREATE URHO SURFACE AND ADD");

            helloWorldUrhoSurface.HandlerChanged += delegate {
                Debug.WriteLine("GOT HANDLER FOR URHO SURFACE");
                startUrho();

            };

            mainPage.SizeChanged += delegate {
                abs1.WidthRequest = abs2.WidthRequest = mainPage.Width;
                abs1.HeightRequest = abs2.HeightRequest = mainPage.Height;
                Debug.WriteLine("SCREEN CHANGED");
                if (helloWorldUrhoSurface != null) {
                    helloWorldUrhoSurface.WidthRequest = mainPage.Width;
                    helloWorldUrhoSurface.HeightRequest = mainPage.Height;
                }
                
            };
        }

        async Task startUrho() {
            Debug.WriteLine("START URHO");
            try {
                var helloWorldApp = await helloWorldUrhoSurface.Show<HelloWorld>(new Urho.ApplicationOptions(assetsFolder: null));
                Debug.WriteLine("GOT HELLO WORLD APP " + helloWorldApp.GetType()); //NOT GETTING HERE
                helloWorldApp.Run();
            }
            catch (Exception ex) {
                Debug.Write(ex);
            }

        }
    }
    public class HelloWorld : Urho.Application {

        //NONE OF THIS IS RUN CURRENTLY
        public HelloWorld(Urho.ApplicationOptions options) : base(options) {
            Debug.WriteLine("HELLO WORLD CONSTRUCTOR");
        }

        protected override async void Start() {
            Debug.WriteLine("HELLO WORLD START");
            base.Start();
            await Create3DObject();
        }

        private async Task Create3DObject() {
            Debug.WriteLine("HELLO WORLD CREATE 3D");
            var scene = new Urho.Scene();
            scene.CreateComponent<Urho.Octree>();

            // Note: Will continue adding code here
            Urho.Node node = scene.CreateChild();
            node.Position = new Urho.Vector3(0, 0, 5);
            node.Rotation = new Urho.Quaternion(60, 0, 30);
            node.SetScale(1f);

            // Add Pyramid Model
            Urho.StaticModel modelObject = node.CreateComponent<Urho.StaticModel>();
            modelObject.Model = ResourceCache.GetModel("Models/Pyramid.mdl");
            
            Urho.Node light = scene.CreateChild(name: "light");
            light.SetDirection(new Urho.Vector3(0.4f, -0.5f, 0.3f));
            light.CreateComponent<Urho.Light>();
            //Urho.LightType.Directional


            Urho.Node cameraNode = scene.CreateChild(name: "camera");
            Urho.Camera camera = cameraNode.CreateComponent<Urho.Camera>();

            Renderer.SetViewport(0, new Urho.Viewport(scene, camera, null));
            await node.RunActionsAsync(
                new Urho.Actions.RepeatForever(
                    new Urho.Actions.RotateBy(duration: 1,deltaAngleX: 0, deltaAngleY: 90, deltaAngleZ: 0
                    )
                )
            );
        }
    }
    
    
}