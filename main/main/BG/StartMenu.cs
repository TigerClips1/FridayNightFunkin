﻿using System;
using System.Diagnostics.Eventing.Reader;
using System.Net.Mail;
using System.Net.Mime;
using System.Numerics;
using System.Xml;
using Orbis.Audio;
using Orbis.Game;
using Orbis.Interfaces;
using OrbisGL;
using OrbisGL.Audio;
using OrbisGL.Controls.Events;
using OrbisGL.GL;
using OrbisGL.GL2D;

namespace Orbis.BG;

public class StartMenu : GLObject2D, ILoadable
{
    private bool Ready;
    private SFXHelper SFX => SFXHelper.Default;
    
    private WavePlayer Theme;
    private OrbisAudioOut ThemeDriver;

    private WavePlayer SFXPlayer;
    private OrbisAudioOut SFXDriver;
    
    private SpriteAtlas2D Logo;
    private SpriteAtlas2D PressStart;
    private SpriteAtlas2D Girlfriend;
    
    public bool Loaded { get; private set; }
    public int TotalProgress { get => 5 + SFX.TotalProgress; }
    public void Load(Action<int> OnProgressChanged)
    {

#if ORBIS
        SFX.Load((i) => { OnProgressChanged(i); });

        Theme = new WavePlayer();
        Theme.SetAudioDriver(ThemeDriver = new OrbisAudioOut());
        Theme.Loop = true;
        Theme.Open(Util.CopyFileToMemory("freakyMenu_48khz.wav"));
#endif
        OnProgressChanged?.Invoke(SFX.TotalProgress + 1);

#if ORBIS
        SFXPlayer = new WavePlayer();
        SFXPlayer.SetAudioDriver(SFXDriver = new OrbisAudioOut());
        SFXPlayer.Open(SFX.GetSFX(SFXType.MenuConfirm));
#endif

        OnProgressChanged?.Invoke(SFX.TotalProgress + 2);
        
        var XML = Util.GetXML("gfDanceTitle.xml");
        Girlfriend = new SpriteAtlas2D(XML, Util.CopyFileToMemory, true);
        Girlfriend.SetActiveAnimation("gfDance");
        Girlfriend.Position = new Vector2(1920, 1080) - new Vector2(Girlfriend.Width, Girlfriend.Height) - new Vector2(50, 50);
        Girlfriend.Position -= new Vector2(350, 200);
        Girlfriend.SetZoom(0.8f);

        OnProgressChanged?.Invoke(SFX.TotalProgress + 3);

        XML = Util.GetXML("logoBumpin.xml");
        Logo = new SpriteAtlas2D(XML, Util.CopyFileToMemory, true);
        Logo.SetActiveAnimation("logo bumpin");
        Logo.Position = new Vector2(-10, -10);
        Logo.SetZoom(0.8f);

        OnProgressChanged?.Invoke(SFX.TotalProgress + 4);

        XML = Util.GetXML("titleEnter.xml");
        PressStart = new SpriteAtlas2D(XML, Util.CopyFileToMemory, true);
        PressStart.SetActiveAnimation("Press Enter to Begin");
        PressStart.Position = new Vector2(50, 1080 - PressStart.Height - 50);

        Rectangle2D Rectangle = new Rectangle2D(1920, 1080, true);
        Rectangle.Color = RGBColor.Black;

        Rectangle.RefreshVertex();


        AddChild(Rectangle);
        AddChild(Logo);
        AddChild(Girlfriend);
        AddChild(PressStart);

#if ORBIS
        Application.Default.Gamepad.OnButtonUp += GamepadOnOnButtonUp;
#endif
        Application.Default.KeyboardDriver.OnKeyUp += OnKeyUp;

        Loaded = true;
        
        OnProgressChanged?.Invoke(SFX.TotalProgress + 5);
    }

    private void OnKeyUp(object Sender, KeyboardEventArgs Args)
    {
        if (Started)
            return;

        if (Args.Keycode == IME_KeyCode.RETURN)
            Confirm();
    }

    private bool Started = false;

    private void GamepadOnOnButtonUp(object sender, ButtonEventArgs args)
    {
        if (Started)
            return;
        
        if (args.Button.HasFlag(OrbisPadButton.Options))
            Confirm();
    }

    private void Confirm()
    {
        Started = true;

        PressStart.SetActiveAnimation("ENTER PRESSED");

        ThemeDriver?.Dispose();

        if (SFXPlayer != null)
        {
            SFXPlayer.OnTrackEnd += OnSFXEnd;
            SFXPlayer.Resume();
        }
        else
        {
            //In PC Debug
            PressStart.OnAnimationEnd += OnSFXEnd;
        }
    }

    void OnSFXEnd(object sender, EventArgs Args)
    {
        //OnTrackEnd is called in a background thread,
        //you can't use to interact with OpenGL
        Ready = true;
    }

    private void StarGame()
    {
        LoadingBG LoadBG = new LoadingBG();
        LoadBG.Load(i =>
        {
            if (!LoadBG.Loaded)
                return;

            Application.Default.RemoveObjects();
            Application.Default.AddObject(LoadBG);
            Dispose();
            
            var Song = new SongPlayer(Util.GetSongByName("Bopeebo"));
            LoadBG.Load(Song, () =>
            {
                Application.Default.RemoveObjects();
                LoadBG.Dispose();


                Song.Begin();
                Application.Default.AddObject(Song);
            });
        });
    }

    private long LastFrameTick = 0;
    private long FrameTick = Constants.ORBIS_MILISECOND * 20;
    public override void Draw(long Tick)
    {
        if (Ready)
        {
            Ready = false;
            StarGame();
        }
        
        if (LastFrameTick == 0)
            Theme?.Resume();
        
        long ElapsedTick = Tick - LastFrameTick;
        if (ElapsedTick > FrameTick)
        {
            LastFrameTick = Tick;
            Logo?.NextFrame();
            Girlfriend?.NextFrame();
            PressStart?.NextFrame();
        }
        
        base.Draw(Tick);
    }
    public override void Dispose()
    {
        Logo?.Dispose();
        Girlfriend?.Dispose();
        Theme?.Dispose();
    }
}