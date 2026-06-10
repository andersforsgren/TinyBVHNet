using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TinyBVHNet.Raytracer;

namespace TinyBVHNet.Raytracer.Ui;

/// <summary>
/// Interactive progressive ray tracer with orbit/pan/zoom camera controls.
/// Renders 1spp per frame, continuously accumulating into an HDR buffer.
/// </summary>
public sealed class MainForm : Form
{
    // -- Settings ----------------------------------------------
    private const int ImageWidth = 1280;
    private const int ImageHeight = 800;

    private bool _useGpu;
    private int _maxBounces = 3;
    private float _exposure = 1.5f;

    // -- Camera state ------------------------------------------
    private Vector3 _lookAt = new(0f, 2f, 0f);
    private float _yaw;          // radians around Y axis
    private float _pitch = 0.3f;  // radians above horizon
    private float _distance = 8f;

    private Camera CurrentCamera
    {
        get
        {
            float x = _distance * MathF.Cos(_pitch) * MathF.Sin(_yaw);
            float y = _distance * MathF.Sin(_pitch);
            float z = _distance * MathF.Cos(_pitch) * MathF.Cos(_yaw);
            var pos = _lookAt + new Vector3(x, y, z);
            return new Camera(pos, _lookAt, Vector3.UnitY, 60f);
        }
    }

    // -- Render state ------------------------------------------
    private IRenderer? _renderer;
    private float[]? _accumBuffer;   // RGB interleaved, length = W*H*3
    private int _sampleCount;
    private long _cameraGeneration;  // bumped when camera moves; stale frames discarded
    private readonly object _bufferLock = new();

    // -- Denoising ---------------------------------------------
    private bool _denoiseEnabled;
    private OidnDenoiser? _denoiser;

    private Bitmap? _displayBitmap;
    private readonly byte[] _displayBytes = new byte[ImageWidth * ImageHeight * 3];
    private readonly int[] _displayPixels = new int[ImageWidth * ImageHeight]; // ARGB packed

    // -- Performance -------------------------------------------
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private double _lastFrameMs;
    private double _mraysPerSec;
    private long _totalRays;

    // -- Convergence ------------------------------------------
    private float[]? _lastAveragedFrame;  // averaged HDR from previous sample
    private double _convergenceDelta;     // mean per-pixel relative luminance error (0..1)

    // -- Render thread -----------------------------------------
    private CancellationTokenSource _renderCts = new();
    private Task? _renderTask;

    // -- Mouse interaction ------------------------------------
    private bool _isDragging;
    private Point _lastMousePos;
    private bool _panMode; // Ctrl held -> pan instead of orbit
    private string scenePath = null!;

    // ==========================================================
    public MainForm()
    {
        Text = "TinyBVHNet -- Interactive Ray Tracer";
        ClientSize = new Size(ImageWidth, ImageHeight);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        DoubleBuffered = true;
        KeyPreview = true;

        _displayBitmap = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format32bppArgb);
    }

    

    // ==========================================================
    //  Lifecycle
    // ==========================================================
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        using var dlg = new OpenFileDialog()
        {
            Filter = "OBJ files (*.obj)|*.obj|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            scenePath = dlg.FileName;
        else
            Close();
      
        if (!File.Exists(scenePath))
        {
            MessageBox.Show($"Scene file not found: {scenePath}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        using var g = CreateGraphics();
        g.Clear(Color.Black);
        DrawOverlay(g, "Loading scene...", 0, 0, 0);
        Text = "TinyBVHNet -- Loading...";

        Task.Run(() =>
        {
            var scene = ObjParser.Load(scenePath);
            float[] accum = new float[ImageWidth * ImageHeight * 3];

            BeginInvoke(() =>
            {
                _renderer = IsCpu
                    ? new Cpu.CpuRenderer(scene, ImageWidth, ImageHeight)
                    : new Gpu.GpuRenderer(scene, ImageWidth, ImageHeight);
                lock (_bufferLock) { _accumBuffer = accum; _sampleCount = 0; }
                Text = $"TinyBVHNet -- {(IsCpu ? "CPU" : "GPU")} | {scene.TriangleCount} tris";
                StartRenderLoopInternal(_renderCts.Token);
            });
        });
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopRenderLoop();
        _renderer?.Dispose();
        _denoiser?.Dispose();
        _displayBitmap?.Dispose();
        base.OnFormClosed(e);
    }

    // ==========================================================
    //  Renderer
    // ==========================================================
    private bool IsCpu => !_useGpu; // GPU requires Vulkan

    private void CreateRenderer(ObjParser.Scene scene)
    {
        _renderer?.Dispose();
        if (IsCpu)
            _renderer = new Cpu.CpuRenderer(scene, ImageWidth, ImageHeight);
        else
            _renderer = new Gpu.GpuRenderer(scene, ImageWidth, ImageHeight);
    }

    private void StartRenderLoopInternal(CancellationToken token)
    {
        _frameTimer.Restart();

        _renderTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var opts = new RenderOptions(_maxBounces);
                Camera cam;
                long gen;
                lock (_bufferLock) { cam = CurrentCamera; gen = _cameraGeneration; }

                float[] hdr = _renderer!.Render(cam, opts);

                lock (_bufferLock)
                {
                    // Stale frame -- camera moved during render
                    if (gen != _cameraGeneration) continue;
                    if (_accumBuffer == null) break;
                    int len = _accumBuffer.Length;

                    // Normalize channels: CPU returns mono (W*H), GPU returns RGB (W*H*3)
                    if (hdr.Length == len / 3) // mono -> replicate to RGB
                    {
                        for (int i = 0; i < hdr.Length; i++)
                        {
                            _accumBuffer[i * 3 + 0] += hdr[i];
                            _accumBuffer[i * 3 + 1] += hdr[i];
                            _accumBuffer[i * 3 + 2] += hdr[i];
                        }
                    }
                    else // already RGB
                    {
                        for (int i = 0; i < len; i++)
                            _accumBuffer[i] += hdr[i];
                    }

                    _sampleCount++;
                    double frameMs = _frameTimer.Elapsed.TotalMilliseconds;
                    _lastFrameMs = frameMs;
                    _frameTimer.Restart();

                    // Estimate MRay/s: W*H rays primary + ~2*bounces shadow/indirect per pixel
                    long raysThisFrame = (long)ImageWidth * ImageHeight * (1 + 2 * _maxBounces);
                    _totalRays += raysThisFrame;
                    _mraysPerSec = raysThisFrame / (frameMs * 1e3); // rays / (ms*1000 -> sec) / 1e6

                    // Tonemap and update display bitmap
                    UpdateDisplayBitmap();
                }

                // Signal UI to repaint
                BeginInvoke(() => Invalidate());
            }
        }, token);
    }

    private void StopRenderLoop()
    {
        _renderCts.Cancel();
        try { _renderTask?.Wait(2000); } catch { /* canceled */ }
    }

    /// <summary>Must be called under _bufferLock.</summary>
    private void UpdateDisplayBitmap()
    {
        if (_accumBuffer == null || _sampleCount == 0) return;

        int len = _accumBuffer.Length;
        float invSpp = 1f / _sampleCount;

        // -- Convergence: mean absolute luminance change, normalized by image mean luminance --
        if (_lastAveragedFrame != null && _lastAveragedFrame.Length == len)
        {
            double totalAbsErr = 0;
            double totalNewLum = 0;
            int px = len / 3;
            for (int i = 0; i < len; i += 3)
            {
                float oldLum = 0.2126f * _lastAveragedFrame[i]
                             + 0.7152f * _lastAveragedFrame[i + 1]
                             + 0.0722f * _lastAveragedFrame[i + 2];
                float nr = _accumBuffer[i]     * invSpp;
                float ng = _accumBuffer[i + 1] * invSpp;
                float nb = _accumBuffer[i + 2] * invSpp;
                float newLum = 0.2126f * nr + 0.7152f * ng + 0.0722f * nb;
                totalAbsErr += Math.Abs(newLum - oldLum);
                totalNewLum += newLum;

                // Store averaged frame for next iteration (combine passes)
                _lastAveragedFrame[i] = nr;
                _lastAveragedFrame[i + 1] = ng;
                _lastAveragedFrame[i + 2] = nb;
            }
            double meanNewLum = totalNewLum / px;
            _convergenceDelta = totalAbsErr / (px * Math.Max(meanNewLum, 0.001));
        }
        else
        {
            // First frame -- just store the averaged frame
            if (_lastAveragedFrame == null || _lastAveragedFrame.Length != len)
                _lastAveragedFrame = new float[len];
            for (int i = 0; i < len; i++)
                _lastAveragedFrame[i] = _accumBuffer[i] * invSpp;
        }

        float[] source = _accumBuffer;

        // Optionally denoise the averaged HDR buffer
        if (_denoiseEnabled && _denoiser != null && _sampleCount > 0)
        {
            // Average into a temp buffer, denoise, then use that for tonemapping
            float[] avg = new float[_accumBuffer.Length];
            for (int i = 0; i < avg.Length; i++)
                avg[i] = _accumBuffer[i] * invSpp;

            try { source = _denoiser.Denoise(avg); }
            catch { /* denoise failed, fall through with original */ }

            invSpp = 1f; // denoised buffer is already averaged
        }

        for (int i = 0; i < _displayPixels.Length; i++)
        {
            float r = RaytracerUtil.TonemapReinhard(source[i * 3 + 0] * invSpp, _exposure);
            float g = RaytracerUtil.TonemapReinhard(source[i * 3 + 1] * invSpp, _exposure);
            float b = RaytracerUtil.TonemapReinhard(source[i * 3 + 2] * invSpp, _exposure);

            int ir = (int)Math.Clamp(r * 255f + 0.5f, 0, 255);
            int ig = (int)Math.Clamp(g * 255f + 0.5f, 0, 255);
            int ib = (int)Math.Clamp(b * 255f + 0.5f, 0, 255);
            _displayPixels[i] = unchecked((int)0xFF000000) | (ir << 16) | (ig << 8) | ib;
        }

        var bmpData = _displayBitmap!.LockBits(
            new Rectangle(0, 0, ImageWidth, ImageHeight),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(_displayPixels, 0, bmpData.Scan0, _displayPixels.Length);
        _displayBitmap.UnlockBits(bmpData);
    }

    private void ResetAccumulation()
    {
        lock (_bufferLock)
        {
            if (_accumBuffer != null)
                Array.Clear(_accumBuffer);
            _sampleCount = 0;
            _totalRays = 0;
            _cameraGeneration++;
            _convergenceDelta = 0;
            _lastAveragedFrame = null;

            // Blit black to the display immediately so the old image disappears
            if (_displayBitmap != null)
            {
                var bmpData = _displayBitmap.LockBits(
                    new Rectangle(0, 0, ImageWidth, ImageHeight),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Array.Clear(_displayPixels, 0, _displayPixels.Length);
                Marshal.Copy(_displayPixels, 0, bmpData.Scan0, _displayPixels.Length);
                _displayBitmap.UnlockBits(bmpData);
            }
        }
        Invalidate();
    }

    // ==========================================================
    //  Paint
    // ==========================================================
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.HighSpeed;

        // Draw the tonemapped bitmap
        if (_displayBitmap != null)
            g.DrawImage(_displayBitmap, ClientRectangle);

        // Overlay
        string rendererName = IsCpu ? "CPU (G)" : "GPU (G)";
        string bouncesTag = $"MaxBounces: {_maxBounces} (F3/F4)";
        string oidnTag = _denoiseEnabled ? "Denoise ON (D)" : "Denoise OFF (D)";
        string convText = _sampleCount > 1
            ? $" | Delta {_convergenceDelta * 100:F2}%"
            : "";
        DrawOverlay(g,
            $"{rendererName} | {bouncesTag} | {oidnTag} | {_sampleCount} spp | {_mraysPerSec:F2} MRay/s | {_lastFrameMs:F1} ms/frame{convText}",
            _sampleCount, _mraysPerSec, _lastFrameMs);
    }

    private static void DrawOverlay(Graphics g, string text, int samples, double mrays, double frameMs)
    {
        const int padX = 8, padY = 6;
        const int fontSize = 12;

        using var font = new Font("Consolas", fontSize, FontStyle.Regular);
        var textSize = g.MeasureString(text, font);
        int boxW = (int)textSize.Width + padX * 2;
        int boxH = (int)textSize.Height + padY * 2;

        // Semi-transparent background
        using var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        g.FillRectangle(bgBrush, 0, 0, boxW, boxH);

        // Text
        using var textBrush = new SolidBrush(Color.White);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.DrawString(text, font, textBrush, padX, padY);
    }

    // ==========================================================
    //  Keyboard
    // ==========================================================
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Control) _panMode = true;

        switch (e.KeyCode)
        {
            case Keys.G:
                _useGpu = !_useGpu;
                RecreateRendererAndRestart();
                break;
            case Keys.OemMinus: case Keys.Subtract:
                _exposure = Math.Max(0.1f, _exposure - 0.2f);
                break;
            case Keys.Oemplus: case Keys.Add:
                _exposure = Math.Min(10f, _exposure + 0.2f);
                break;
            case Keys.F3:
                _maxBounces = Math.Max(0, _maxBounces - 1);
                ResetAccumulation();
                break;
            case Keys.F4:
                _maxBounces = Math.Min(16, _maxBounces + 1);
                ResetAccumulation();
                break;
            case Keys.R:
                ResetAccumulation();
                break;
            case Keys.D:
                _denoiseEnabled = !_denoiseEnabled;
                if (_denoiseEnabled && _denoiser == null)
                {
                    try { _denoiser = new OidnDenoiser(ImageWidth, ImageHeight); }
                    catch (Exception ex) { _denoiseEnabled = false; MessageBox.Show($"OIDN init failed: {ex.Message}", "Denoiser", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
                break;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!e.Control) _panMode = false;
    }

    private void RecreateRendererAndRestart()
    {
        _renderCts.Cancel();
        try { _renderTask?.Wait(2000); } catch { /* canceled */ }
        if (_renderer == null) return;

        float[] accum = new float[ImageWidth * ImageHeight * 3];
        var scene = ObjParser.Load(scenePath);
        _renderer?.Dispose();

        if (IsCpu)
            _renderer = new Cpu.CpuRenderer(scene, ImageWidth, ImageHeight);
        else
            _renderer = new Gpu.GpuRenderer(scene, ImageWidth, ImageHeight);

        lock (_bufferLock) { _accumBuffer = accum; _sampleCount = 0; }
        _totalRays = 0;
        _convergenceDelta = 0;
        _lastAveragedFrame = null;

        // Re-create denoiser if enabled
        _denoiser?.Dispose();
        _denoiser = null;
        if (_denoiseEnabled)
        {
            try { _denoiser = new OidnDenoiser(ImageWidth, ImageHeight); }
            catch { _denoiseEnabled = false; }
        }

        Text = $"TinyBVH .NET Raytracer demo";

        _renderCts.Dispose();
        _renderCts = new CancellationTokenSource();
        StartRenderLoopInternal(_renderCts.Token);
    }

    // ==========================================================
    //  Mouse -- camera controls
    // ==========================================================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastMousePos = e.Location;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            _isDragging = false;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging) return;

        int dx = e.X - _lastMousePos.X;
        int dy = e.Y - _lastMousePos.Y;
        _lastMousePos = e.Location;

        const float sensitivity = 0.005f;

        if (_panMode)
        {
            // Pan: move look-at and camera together perpendicular to view
            var cam = CurrentCamera;
            var forward = Vector3.Normalize(cam.LookAt - cam.Pos);
            var right = Vector3.Normalize(Vector3.Cross(forward, cam.Up));
            var up = Vector3.Cross(right, forward);
            var offset = (-dx * right + dy * up) * sensitivity * _distance;
            _lookAt += offset;
        }
        else
        {
            // Orbit: rotate around look-at
            _yaw -= dx * sensitivity;
            _pitch += dy * sensitivity;
            _pitch = Math.Clamp(_pitch, -MathF.PI / 2f + 0.05f, MathF.PI / 2f - 0.05f);
        }

        ResetAccumulation();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float zoom = e.Delta > 0 ? 0.9f : 1.1f;
        _distance = Math.Clamp(_distance * zoom, 0.5f, 100f);
        ResetAccumulation();
    }
}
