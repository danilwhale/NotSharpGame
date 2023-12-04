using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

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
        public const string TexcoordAttribute = "aTexcoord";
        
        public Vector3[] Vertices;
        public uint[] Indices;
        public Color[] Colors;
        public Vector2[] Texcoords;
        
        public readonly uint Ebo, Vao;
        
        /*
         * Vbo - vertices buffer
         * Cbo - colors buffer
         * Tbo - texcoords buffer
         */
        public readonly uint Vbo, Cbo, Tbo;

        private int _vertexAttrib = -1;
        private int _colorAttrib = -1;
        private int _texcoordAttrib = -1;

        public Mesh()
        {
            Ebo = _gl.CreateBuffer();
            Vao = _gl.CreateVertexArray();
            
            Vbo = _gl.CreateBuffer();
            Cbo = _gl.CreateBuffer();
            Tbo = _gl.CreateBuffer();
        }

        public void FetchAttributesFromShader(Shader shader)
        {
            _vertexAttrib = _gl.GetAttribLocation(shader.Id, VertexAttribute);
            ThrowIfInvalidAttrib(_vertexAttrib, "vertex");
            
            _colorAttrib = _gl.GetAttribLocation(shader.Id, ColorAttribute);
            ThrowIfInvalidAttrib(_colorAttrib, "color");

            _texcoordAttrib = _gl.GetAttribLocation(shader.Id, TexcoordAttribute);
            ThrowIfInvalidAttrib(_colorAttrib, "texcoord");
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
            if (Vertices == null || Indices == null || Texcoords == null)
            {
                throw new NullReferenceException("one of your buffers is null you bozo");
            }

            if (_vertexAttrib < 0 || _colorAttrib < 0 || _texcoordAttrib < 0)
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
            
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, Tbo);
            _gl.BufferData<Vector2>(BufferTargetARB.ArrayBuffer, Texcoords, BufferUsageARB.StaticDraw);
            
            _gl.VertexAttribPointer((uint)_texcoordAttrib, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray((uint)_texcoordAttrib);
            
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
            _gl.DeleteBuffer(Tbo);
        }
    }

    public struct Color
    {
        public byte R;

        public byte G;

        public byte B;

        public byte A;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            R = Convert.ToByte(r);
            G = Convert.ToByte(g);
            B = Convert.ToByte(b);
            A = Convert.ToByte(a);
        }

        public Color(float r, float g, float b, float a = 1.0f)
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

    public class Texture : IDisposable
    {
        public readonly int Width;
        public readonly int Height;
        public readonly PixelFormat Format;
        
        public readonly uint Id;

        public Texture(Image image)
        {
            Id = _gl.GenTexture();
            
            Bind();
            
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            _gl.TexImage2D<byte>(TextureTarget.Texture2D, 0, (int)image.Format, image.Width, image.Height, 0, image.Format, PixelType.UnsignedByte, image.Data);
            _gl.GenerateMipmap(TextureTarget.Texture2D);
            
            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Bind()
        {
            _gl.BindTexture(TextureTarget.Texture2D, Id);
        }

        public void Dispose()
        {
            _gl.DeleteTexture(Id);
        }
    }

    public class Image
    {
        public byte[] Data;
        public uint Width;
        public uint Height;
        public PixelFormat Format;

        public Image(byte[] data, uint width, uint height, PixelFormat format)
        {
            Data = data;
            Width = width;
            Height = height;
            Format = format;
        }

        public Image(string path)
        {
            ImageResult img = ImageResult.FromMemory(File.ReadAllBytes(path));
            Data = img.Data;
            Width = (uint)img.Width;
            Height = (uint)img.Height;
            Format = img.Comp switch
            {
                ColorComponents.RedGreenBlue => PixelFormat.Rgb,
                ColorComponents.RedGreenBlueAlpha => PixelFormat.Rgba,
                _ => throw new ArgumentException("unsupported color component")
            };
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;

            int i = (int)(x + Width * y);
            Data[i + 0] = color.R;
            Data[i + 1] = color.G;
            Data[i + 2] = color.B;
            if (Format == PixelFormat.Rgba) Data[i + 3] = color.A;
        }

        public Color GetPixel(int x, int y)
        {
            Color color = new Color();
            if (x < 0 || x >= Width || y < 0 || y >= Height) return color;
            
            int i = (int)(x + Width * y);
            color.R = Data[i + 0];
            color.G = Data[i + 1];
            color.B = Data[i + 2];
            color.A = Format == PixelFormat.Rgba ? Data[i + 3] : (byte)255;

            return color;
        }
    }

    private static Shader _shader;
    private static Mesh _mesh;
    private static Texture _texture;

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
            layout (location = 2) in vec2 aTexcoord;
            
            out vec4 color;
            out vec2 texcoord;
            
            void main()
            {
                gl_Position = vec4(aVertex, 1.0);
                
                color = aColor;
                texcoord = aTexcoord;
            }  
            """,
            """
            #version 330 core
            out vec4 fragColor;
            
            in vec4 color;
            in vec2 texcoord;
            
            uniform sampler2D uTexture;
            
            void main()
            {
                fragColor = texture(uTexture, texcoord) * color;
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
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(255, 255, 255)
        };
        _mesh.Texcoords = new Vector2[]
        {
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0),
            new Vector2(0, 1)
        };
        
        _mesh.FetchAttributesFromShader(_shader);
        _mesh.UpdateBuffers();

        _texture = new Texture(new Image("Resources/Grass.png"));
        
        _gl.ClearColor(System.Drawing.Color.CornflowerBlue);
    }
    
    private static void Update(double delta)
    {
        _window.Title = string.Format(TitleFormat, Math.Round(1 / delta));
    }

    private static void Render(double delta)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        
        _texture.Bind();
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
        _texture.Dispose();
        
        _gl.Dispose();
        _input.Dispose();
    }
}