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
        public const string VertexAttribute = "aVertex";
        public const string ColorAttribute = "aColor";
        
        public Vector3[] Vertices;
        public uint[] Indices;
        public Color[] Colors;
        
        public readonly uint Ebo, Vao;
        
        /*
         * Vbo - vertices buffer
         * Cbo - colors buffer
         */
        public readonly uint Vbo, Cbo;

        private int _vertexAttrib = -1;
        private int _colorAttrib = -1;

        public Mesh()
        {
            Ebo = _gl.CreateBuffer();
            Vao = _gl.CreateVertexArray();
            
            Vbo = _gl.CreateBuffer();
            Cbo = _gl.CreateBuffer();
        }

        public void FetchAttributesFromShader(Shader shader)
        {
            _vertexAttrib = _gl.GetAttribLocation(shader.Id, VertexAttribute);
            ThrowIfInvalidAttrib(_vertexAttrib, "vertex");
            
            _colorAttrib = _gl.GetAttribLocation(shader.Id, ColorAttribute);
            ThrowIfInvalidAttrib(_colorAttrib, "color");
        }

        private void ThrowIfInvalidAttrib(int attrib, string name)
        {
            if (attrib < 0)
            {
                throw new IndexOutOfRangeException("attribute " + name + " doesnt exist");
            }
        }

        public unsafe void UpdateBuffers()
        {
            if (Vertices == null || Indices == null)
            {
                throw new NullReferenceException("one of your buffers is null you bozo");
            }

            if (_vertexAttrib < 0 || _colorAttrib < 0)
            {
                throw new IndexOutOfRangeException("attributes arent fetched");
            }
            
            _gl.BindVertexArray(Vao);
            
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            _gl.BufferData<Vector3>(BufferTargetARB.ArrayBuffer, Vertices, BufferUsageARB.StaticDraw);

            _gl.VertexAttribPointer((uint)_vertexAttrib, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray((uint)_vertexAttrib);
            
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Cbo);
            _gl.BufferData<Color>(BufferTargetARB.ArrayBuffer, Colors, BufferUsageARB.StaticDraw);
            
            _gl.VertexAttribPointer((uint)_colorAttrib, 4, VertexAttribPointerType.UnsignedByte, true, 4 * sizeof(byte), (void*)0);
            _gl.EnableVertexAttribArray((uint)_colorAttrib);
            
            if (Vertices.Length != 0)
            {
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
                _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, Indices, BufferUsageARB.StaticDraw);
            }

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
            _gl.DeleteBuffer(Ebo);
            _gl.DeleteVertexArray(Vao);
            
            _gl.DeleteBuffer(Vbo);
            _gl.DeleteBuffer(Cbo);
        }
    }

    public struct Color
    {
        public byte R;

        public byte G;

        public byte B;

        public byte A;

        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b, int a)
        {
            R = Convert.ToByte(r);
            G = Convert.ToByte(g);
            B = Convert.ToByte(b);
            A = Convert.ToByte(a);
        }

        public Color(float r, float g, float b, float a)
        {
            R = Convert.ToByte(r * 255);
            G = Convert.ToByte(g * 255);
            B = Convert.ToByte(b * 255);
            A = Convert.ToByte(a * 255);
        }

        public static implicit operator Color(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static implicit operator System.Drawing.Color(Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
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
            layout (location = 1) in vec4 aColor;
            
            out vec4 color;
            
            void main()
            {
                gl_Position = vec4(aVertex, 1.0);
                color = aColor;
            }  
            """,
            """
            #version 330 core
            out vec4 fragColor;
            
            in vec4 color;
            
            void main()
            {
                fragColor = color;
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
        _mesh.Colors = new Color[]
        {
            new Color(255, 0, 0, 255),
            new Color(0, 255, 0, 255),
            new Color(0, 0, 255, 255),
            new Color(255, 255, 0, 255)
        };
        _mesh.FetchAttributesFromShader(_shader);
        _mesh.UpdateBuffers();
        
        _gl.ClearColor(System.Drawing.Color.CornflowerBlue);
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
        _mesh.Dispose();
        _shader.Dispose();
        
        _gl.Dispose();
        _input.Dispose();
    }
}