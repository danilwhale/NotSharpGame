using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

public static class NotSharpGame
{
    private const string TitleFormat = "!#Game - {0} FPS";
    private const string InitTitle = "!#Game";
    
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static IInputContext _input = null!;

    private static void Main()
    {
        var options = WindowOptions.Default;
        options.VSync = true; // so gpu wont burn
        options.Title = InitTitle;
        options.Size = new Vector2D<int>(1280, 720);

        _window = Window.Create(options);
        
        // bind every event we want to
        _window.Load += Initialize;
        _window.Update += Update;
        _window.Render += Render;
        _window.FramebufferResize += FramebufferResize;
        _window.Closing += Destroy;
        
        // and run it
        _window.Run();
    }
    
    // most useful game loop methods are here!11!1 (except Destroy, its at the bottom lmao)
    private static void Initialize()
    {
        _window.Center();
        
        _gl = _window.CreateOpenGL();
        _input = _window.CreateInput();

        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
        }
        
        _gl.ClearColor(Color.CornflowerBlue);
    }
    
    private static void Update(double delta)
    {
        _window.Title = string.Format(TitleFormat, Math.Round(1 / delta));
    }

    private static void Render(double delta)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }
    
    // every other method goes here
    private static void FramebufferResize(Vector2D<int> newSize)
    {
        _gl.Viewport(newSize);
    }
    
    private static void KeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape)
            _window.Close();
    }

    /*
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES
     * PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES PLEASE DONT FORGET TO DISPOSE RESOURCES 
     */ 
    private static void Destroy()
    {
        _gl.Dispose();
        _input.Dispose();
    }
}