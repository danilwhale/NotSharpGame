using System.Drawing;
using System.Numerics;
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

    public class Shader : IDisposable
    {
        public readonly uint Id;

        public Shader(string vsCode, string fsCode)
        {
            uint vs = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vs, vsCode);
            _gl.CompileShader(vs);
            if (_gl.GetShader(vs, ShaderParameterName.CompileStatus) == 0)
            {
                Console.WriteLine(_gl.GetShaderInfoLog(vs));
                return;
            }

            uint fs = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fs, fsCode);
            _gl.CompileShader(fs);
            if (_gl.GetShader(fs, ShaderParameterName.CompileStatus) == 0)
            {
                Console.WriteLine(_gl.GetShaderInfoLog(fs));
                return;
            }

            Id = _gl.CreateProgram();
            _gl.AttachShader(Id, vs);
            _gl.AttachShader(Id, fs);
            _gl.LinkProgram(Id);
            if (_gl.GetProgram(Id, ProgramPropertyARB.LinkStatus) == 0)
            {
                Console.WriteLine(_gl.GetProgramInfoLog(Id));
                Id = 0;
            }
            
            _gl.DeleteShader(vs);
            _gl.DeleteShader(fs);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(Id);
        }
    }

    public class Mesh : IDisposable
    {
        public Vector3[] Vertices;
        public uint[] Indices;
        
        public readonly uint Vbo, Ebo, Vao;

        public Mesh()
        {
            Vbo = _gl.CreateBuffer();
            Ebo = _gl.CreateBuffer();
            Vao = _gl.CreateVertexArray();
        }

        public unsafe void UpdateBuffers()
        {
            if (Vertices == null || Indices == null)
            {
                throw new NullReferenceException("one of your buffers is null you bozo");
            }
            
            _gl.BindVertexArray(Vao);
            
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            _gl.BufferData<Vector3>(BufferTargetARB.ArrayBuffer, Vertices, BufferUsageARB.StaticDraw);

            if (Vertices.Length != 0)
            {
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
                _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, Indices, BufferUsageARB.StaticDraw);
            }
            
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.BindVertexArray(0);
            
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        public unsafe void Draw(PrimitiveType type)
        {
            _gl.BindVertexArray(Vao);

            if (Indices.Length != 0)
            {
                _gl.DrawElements(type, (uint)Indices.Length, DrawElementsType.UnsignedInt, (void*)0);
            }
            else
            {
                _gl.DrawArrays(type, 0, (uint)Vertices.Length);
            }
            
            _gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(Vbo);
            _gl.DeleteVertexArray(Vao);
        }
    }

    private static Shader _shader;
    private static Mesh _mesh;

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

        _shader = new Shader(
            """
            #version 330 core
            layout (location = 0) in vec3 aVertex;
            
            void main()
            {
                gl_Position = vec4(aVertex, 1.0);
            }  
            """,
            """
            #version 330 core
            out vec4 fragColor;
            
            void main()
            {
                fragColor = vec4(1.0, 1.0, 1.0, 1.0);
            }
            """
        );

        _mesh = new Mesh();
        _mesh.Vertices = new Vector3[]
        {
            new Vector3(0.5f, 0.5f, 0.0f),
            new Vector3(0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f)
        };
        _mesh.Indices = new uint[]
        {
            0, 1, 3,
            1, 2, 3
        };
        _mesh.UpdateBuffers();
        
        _gl.ClearColor(Color.CornflowerBlue);
    }
    
    private static void Update(double delta)
    {
        _window.Title = string.Format(TitleFormat, Math.Round(1 / delta));
    }

    private static void Render(double delta)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        
        _gl.UseProgram(_shader.Id);
        _mesh.Draw(PrimitiveType.Triangles);
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
        _shader.Dispose();
        
        _gl.Dispose();
        _input.Dispose();
    }
}