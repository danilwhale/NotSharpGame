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

    private static float _delta = 0;

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

        public int GetAttribLoc(string name)
        {
            int loc = _gl.GetAttribLocation(Id, name);
            if (loc < 0)
            {
                throw new ArgumentException("attribute with specified name cant be found", nameof(name));
            }

            return loc;
        }

        public int GetUniformLoc(string name)
        {
            int loc = _gl.GetUniformLocation(Id, name);
            if (loc < 0)
            {
                throw new ArgumentException("uniform with specified name cant be found", nameof(name));
            }

            return loc;
        }

        public void Use()
        {
            _gl.UseProgram(Id);
        }

        public void SetUniform(string name, bool value)
        {
            _gl.Uniform1(GetUniformLoc(name), value ? 1 : 0);
        }

        public void SetUniform(string name, int value)
        {
            _gl.Uniform1(GetUniformLoc(name), value);
        }

        public void SetUniform(string name, float value)
        {
            _gl.Uniform1(GetUniformLoc(name), value);
        }

        public void SetUniform(string name, Vector2 v)
        {
            _gl.Uniform2(GetUniformLoc(name), v);
        }

        public void SetUniform(string name, Vector3 v)
        {
            _gl.Uniform3(GetUniformLoc(name), v);
        }

        public void SetUniform(string name, Vector4 v)
        {
            _gl.Uniform4(GetUniformLoc(name), v);
        }

        public unsafe void SetUniform(string name, Matrix4x4 m)
        {
            _gl.UniformMatrix4(GetUniformLoc(name), 1, false, (float*)&m);
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
        
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public uint[] Indices = Array.Empty<uint>();
        public Color[] Colors = Array.Empty<Color>();
        public Vector2[] Texcoords = Array.Empty<Vector2>();
        
        public readonly uint Vao;
        
        public readonly Buffer<uint> IndicesBuffer = new Buffer<uint>(BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        public readonly Buffer<Vector3> VerticesBuffer = new Buffer<Vector3>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        public readonly Buffer<Color> ColorsBuffer = new Buffer<Color>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        public readonly Buffer<Vector2> TexcoordsBuffer = new Buffer<Vector2>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StreamDraw);

        private int _vertexAttrib = -1;
        private int _colorAttrib = -1;
        private int _texcoordAttrib = -1;

        public Mesh()
        {
            Vao = _gl.CreateVertexArray();
        }

        public void FetchAttributesFromShader(Shader shader)
        {
            _vertexAttrib = shader.GetAttribLoc(VertexAttribute);
            _colorAttrib = shader.GetAttribLoc(ColorAttribute);
            _texcoordAttrib = shader.GetAttribLoc(TexcoordAttribute);
        }

        public unsafe void UpdateBuffers()
        {
            // we can ignore indices length, they are optional
            if (Vertices.Length < 1 || Colors.Length < 1 || Texcoords.Length < 1)
            {
                throw new NullReferenceException("one of your buffers is null you bozo");
            }

            if (_vertexAttrib < 0 || _colorAttrib < 0 || _texcoordAttrib < 0)
            {
                throw new IndexOutOfRangeException("attributes arent fetched");
            }
            
            _gl.BindVertexArray(Vao);

            if (Indices.Length != 0)
            {
                IndicesBuffer.Bind();
                IndicesBuffer.Upload(Indices);
            }
            
            VerticesBuffer.Bind();
            VerticesBuffer.Upload(Vertices);
            VerticesBuffer.BindAttrib(_vertexAttrib, 3, VertexAttribPointerType.Float, false);
            
            ColorsBuffer.Bind();
            ColorsBuffer.Upload(Colors);
            ColorsBuffer.BindAttrib(_colorAttrib, 4, VertexAttribPointerType.UnsignedByte, true);
            
            TexcoordsBuffer.Bind();
            TexcoordsBuffer.Upload(Texcoords);
            TexcoordsBuffer.BindAttrib(_texcoordAttrib, 2, VertexAttribPointerType.Float, false);
            
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
            _gl.DeleteVertexArray(Vao);
            
            IndicesBuffer.Dispose();
            VerticesBuffer.Dispose();
            ColorsBuffer.Dispose();
            TexcoordsBuffer.Dispose();
        }
    }

    public class Buffer<T> : IDisposable
        where T : unmanaged
    {
        public readonly uint Id;
        public readonly BufferTargetARB Target;
        public readonly BufferUsageARB Usage;

        public Buffer(BufferTargetARB target, BufferUsageARB usage)
        {
            Id = _gl.CreateBuffer();
            Target = target;
            Usage = usage;
        }

        public void Bind()
        {
            _gl.BindBuffer(Target, Id);
        }

        public void Upload(ReadOnlySpan<T> data)
        {
            _gl.BufferData(Target, data, Usage);
        }

        public unsafe void BindAttrib(int index, int size, VertexAttribPointerType type, bool normalized, uint stride = 1, nint offset = 0)
        {
            _gl.VertexAttribPointer((uint)index, size, type, normalized, stride * (uint)sizeof(T), (void*)(offset * sizeof(T)));
            _gl.EnableVertexAttribArray((uint)index);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(Id);
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
    }

    public class Camera
    {
        public Vector3 Position;
        public Vector2 Rotation; // we can use 2d vector for rotation, because we dont use roll value

        public Vector3 Forward = new Vector3(0, 0, -1);
        public Vector3 Up;
        public Vector3 WorldUp;
        public Vector3 Right;
        
        public float Near = 0.1f;
        public float Far = 1000.0f;
        public float Fov;

        public Camera(Vector3 position, Vector3 up, float fov)
        {
            Position = position;
            WorldUp = up;
            Fov = fov;
            Rotation = new Vector2(0.0f, -90.0f);
            UpdateVectors();
        }

        public void UpdateVectors()
        {
            // convert rotation to radians initally, so we wont convert same values multiple times
            float yaw = MathHelper.ToRadians(Rotation.Y);
            float pitch = MathHelper.ToRadians(Rotation.X);
            // float roll = MathHelper.ToRadians(Rotation.Z);
            
            Vector3 forward = new Vector3(
                MathF.Cos(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Sin(yaw) * MathF.Cos(pitch)
            );
            Forward = Vector3.Normalize(forward);

            Right = Vector3.Normalize(Vector3.Cross(Forward, WorldUp));
            Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
        }

        public Matrix4x4 GetProjection()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(Fov * (MathF.PI / 180), (float)_window.Size.X / _window.Size.Y, Near, Far);
        }

        public Matrix4x4 GetView()
        {
            return Matrix4x4.CreateLookAt(Position, Position + Forward, Up);
        }
    }

    public class CameraController
    {
        public Camera Camera;
        public float MouseSensitivity;
        public float Speed;

        private bool _firstFrame = true;
        private Vector2 _lastPosition;

        public CameraController(Camera camera, float mouseSensitivity, float speed)
        {
            Camera = camera;
            MouseSensitivity = mouseSensitivity;
            Speed = speed;

            foreach (var mouse in _input.Mice)
            {
                mouse.MouseMove += HandleMouse;
                mouse.Cursor.CursorMode = CursorMode.Disabled;
            }
        }

        private void HandleMouse(IMouse mouse, Vector2 position)
        {
            // so camera rotation wont bug
            if (_firstFrame)
            {
                _lastPosition = position;
                _firstFrame = false;
            }

            Vector2 mouseDelta = position - _lastPosition;
            _lastPosition = position;

            Camera.Rotation += new Vector2(-mouseDelta.Y, mouseDelta.X) * MouseSensitivity;
            
            // clamp up/down rotation to 90 degrees
            if (Camera.Rotation.X < -89) Camera.Rotation.X = -89;
            else if (Camera.Rotation.X > 89) Camera.Rotation.X = 89;
            
            _camera.UpdateVectors();
        }

        public void Update()
        {
            float velocity = Speed * _delta;
            
            if (Input.IsKeyDown(Key.W))
            {
                Camera.Position += Camera.Forward * velocity;
            }

            if (Input.IsKeyDown(Key.S))
            {
                Camera.Position -= Camera.Forward * velocity;
            }

            if (Input.IsKeyDown(Key.A))
            {
                Camera.Position -= Camera.Right * velocity;
            }

            if (Input.IsKeyDown(Key.D))
            {
                Camera.Position += Camera.Right * velocity;
            }

            if (Input.IsKeyDown(Key.E))
            {
                Camera.Position += Camera.Up * velocity;
            }
            
            if (Input.IsKeyDown(Key.Q))
            {
                Camera.Position -= Camera.Up * velocity;
            }
        }
    }
    
    // shrimple input manager
    public static class Input
    {
        private static Dictionary<Key, bool> _keymap = new Dictionary<Key, bool>();
        
        public static void Initialize()
        {
            foreach (var keyboard in _input.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
            }
        }

        private static void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
        {
            _keymap[key] = false;
        }

        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            _keymap[key] = true;
        }

        public static bool IsKeyDown(Key key)
        {
            return _keymap.TryGetValue(key, out bool down) && down;
        }

        public static bool IsKeyUp(Key key)
        {
            return !IsKeyDown(key);
        }
    }

    // some useful gaying (gaming for straight people) mathematical methods
    public static class MathHelper
    {
        public static float ToRadians(float deg)
        {
            return deg * (MathF.PI / 180);
        }

        public static float ToDegrees(float rad)
        {
            return rad * (180 / MathF.PI);
        }
    }

    private static Shader _shader;
    private static Mesh _mesh;
    private static Texture _texture;
    private static Camera _camera;
    private static CameraController _controller;

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

        _shader = new Shader(File.ReadAllText("Resources/Shader.vert"), File.ReadAllText("Resources/Shader.frag"));

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

        _camera = new Camera(new Vector3(0, 0, 1), Vector3.UnitY, 70.0f);
        _controller = new CameraController(_camera, 0.2f, 2.5f);
        
        Input.Initialize();
        
        _gl.Enable(EnableCap.DepthTest);
        _gl.ClearColor(System.Drawing.Color.CornflowerBlue);
    }
    
    private static void Update(double delta)
    {
        _delta = (float)delta;
        
        _window.Title = string.Format(TitleFormat, Math.Round(1 / delta));
        _controller.Update();
    }

    private static unsafe void Render(double delta)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        _texture.Bind();
        _shader.Use();
        
        Matrix4x4 model = Matrix4x4.CreateFromAxisAngle(new Vector3(0.1f, 0.2f, 0.3f), (float)_window.Time);
        Matrix4x4 view = _camera.GetView();
        Matrix4x4 projection = _camera.GetProjection();
        
        _shader.SetUniform("model", model);
        _shader.SetUniform("view", view);
        _shader.SetUniform("projection", projection);
        
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