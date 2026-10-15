using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Presentation;

/// <summary>
/// One weather particle, from EasyRPG's <c>Game_Screen::Particle</c>.
/// </summary>
public struct Rm2kWeatherParticle
{
    /// <summary>Frames of life left; the particle is alive while it is positive.</summary>
    public int T;

    /// <summary>The particle's column, in screen pixels.</summary>
    public int X;

    /// <summary>The particle's row, in screen pixels.</summary>
    public int Y;

    /// <summary>The particle's own alpha, which the sandstorm draws with.</summary>
    public int Alpha;

    /// <summary>The particle's horizontal velocity, used by the sandstorm.</summary>
    public double Vx;

    /// <summary>The particle's vertical velocity, used by the sandstorm.</summary>
    public double Vy;

    /// <summary>The particle's horizontal acceleration, used by the sandstorm.</summary>
    public double Ax;

    /// <summary>The particle's vertical acceleration, used by the sandstorm.</summary>
    public double Ay;
}

/// <summary>
/// The weather's particles and their per-frame movement, from EasyRPG's
/// <c>Game_Screen</c> weather updates and <c>src/weather.cpp</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a particle system and not a screen pass</strong>, which is why
/// the weather is its own card: rain, snow, fog and sandstorm each have their own
/// per-frame rule, their own particle count and their own drawing. The state existed
/// and nothing drew it, so a game that turned on rain showed a dry screen.
/// </para>
/// <para>
/// <strong>And the randomness is a dedicated deterministic stream</strong>, not the
/// engine's global one. The reference draws its respawn positions and angles from
/// <c>Rand</c>, whose sequence is not reproducible across runs or versions; this
/// runtime's whole point is that a frame is a function of its inputs, so the weather
/// carries its own stream and the tests can pin the numbers. The rules and the ranges
/// are the reference's, only the stream is ours -- and the divergence is stated here
/// rather than hidden.
/// </para>
/// </remarks>
public sealed class Rm2kWeatherState
{
    /// <summary>Weather off.</summary>
    public const int TypeNone = 0;

    /// <summary>Rain.</summary>
    public const int TypeRain = 1;

    /// <summary>Snow.</summary>
    public const int TypeSnow = 2;

    /// <summary>Fog.</summary>
    public const int TypeFog = 3;

    /// <summary>Sandstorm.</summary>
    public const int TypeSandstorm = 4;

    /// <summary>The particles rain and snow use, indexed by the strength.</summary>
    public static readonly int[] RainOrSnowParticles = { 20, 60, 100 };

    /// <summary>The particles a sandstorm uses, indexed by the strength.</summary>
    public static readonly int[] SandParticles = { 128, 191, 255 };

    /// <summary>How many particles fog uses, and how many sandstorm reuses.</summary>
    public const int FogParticles = 2;

    /// <summary>The strengths the editor writes.</summary>
    public const int NumStrength = 3;

    /// <summary>The screen's width in tiles, which bounds a particle's spawn.</summary>
    public const int PanLimitTilesX = 20;

    /// <summary>The screen's height in tiles, which bounds a particle's spawn.</summary>
    public const int PanLimitTilesY = 15;

    private readonly List<Rm2kWeatherParticle> _particles = new();
    private Rm2kEventRandom _random = new();

    /// <summary>The particles, in the reference's own order.</summary>
    public IReadOnlyList<Rm2kWeatherParticle> Particles => _particles;

    /// <summary>
    /// How many particles a type needs, from <c>Weather::GetMaxNumParticles</c>.
    /// </summary>
    /// <param name="pType">The weather type.</param>
    /// <returns>The particle count, or zero for weather that is off.</returns>
    /// <remarks>
    /// <strong>And it is the maximum and not the drawn count</strong>: the reference
    /// sizes its particle vector with this and draws
    /// <c>RainOrSnowParticles[Clamp(strength, 0, 2)]</c> of them, which is why the
    /// table's last entry is not "the count at strength three".
    /// </remarks>
    public static int MaxParticles(int pType)
    {
        return pType switch
        {
            TypeRain or TypeSnow => RainOrSnowParticles[NumStrength - 1],
            TypeFog => FogParticles,
            TypeSandstorm => SandParticles[NumStrength - 1],
            _ => 0,
        };
    }

    /// <summary>How many particles are drawn for a type and strength.</summary>
    /// <param name="pType">The weather type.</param>
    /// <param name="pStrength">The strength.</param>
    /// <returns>The drawn count.</returns>
    public static int DrawnParticles(int pType, int pStrength)
    {
        var strength = Math.Clamp(pStrength, 0, NumStrength - 1);
        return pType switch
        {
            TypeRain or TypeSnow => RainOrSnowParticles[strength],
            TypeFog => FogParticles,
            TypeSandstorm => SandParticles[strength],
            _ => 0,
        };
    }

    /// <summary>
    /// Grows the particle list for a type and scatters the new ones, from
    /// <c>Game_Screen::InitParticles</c>.
    /// </summary>
    /// <param name="pType">The weather type.</param>
    /// <remarks>
    /// <strong>And only the new particles are scattered</strong>: the reference returns
    /// early when the vector is already large enough, so switching from rain to
    /// sandstorm keeps the particles that were already there and only scatters the
    /// ones it adds. A reader that re-scattered all of them would make the sky jump
    /// every time a game changed the weather.
    /// </remarks>
    public void Seed(int pType)
    {
        var needed = MaxParticles(pType);
        if (needed <= _particles.Count)
        {
            return;
        }
        for (var index = _particles.Count; index < needed; index++)
        {
            _particles.Add(new Rm2kWeatherParticle
            {
                T = _random.NextInclusive(0, 39),
                X = _random.NextInclusive(0, PanLimitTilesX - 1),
                Y = _random.NextInclusive(0, PanLimitTilesY - 1),
            });
        }
    }

    /// <summary>
    /// One frame of the weather, from the reference's <c>UpdateWeather</c> dispatch.
    /// </summary>
    /// <param name="pType">The weather type.</param>
    /// <param name="pStrength">The strength.</param>
    /// <remarks>
    /// <strong>And each type has its own rule</strong>: rain falls four rows and one
    /// column left per frame and respawns one time in ten with twelve frames of life;
    /// snow drifts a random column left and two or three rows down and respawns one
    /// time in twenty with thirty frames; fog only advances its two frame counters; and
    /// a sandstorm does both the fog and a full particle simulation with velocities and
    /// accelerations.
    /// </remarks>
    public void Update(int pType, int pStrength)
    {
        switch (pType)
        {
            case TypeRain:
                UpdateRain();
                break;
            case TypeSnow:
                UpdateSnow();
                break;
            case TypeFog:
                UpdateFog();
                break;
            case TypeSandstorm:
                UpdateSandstorm();
                break;
        }
    }

    /// <summary>
    /// Replaces one particle, for tests that need a known sky.
    /// </summary>
    /// <param name="pIndex">The particle's index.</param>
    /// <param name="pParticle">The particle.</param>
    /// <remarks>
    /// <strong>And this exists because the scatter is random</strong>: a test that
    /// wants to pin one frame's arithmetic needs one particle it placed itself, and the
    /// alternative -- reproducing the stream's first numbers in the test -- would pin
    /// the generator instead of the rule.
    /// </remarks>
    internal void SetParticle(int pIndex, Rm2kWeatherParticle pParticle)
    {
        while (_particles.Count <= pIndex)
        {
            _particles.Add(default);
        }
        _particles[pIndex] = pParticle;
    }

    /// <summary>Drops every particle, for a new game.</summary>
    /// <remarks>
    /// <strong>And a new game starts with no particles at all</strong>, because the
    /// weather it starts with is none. The reference leaves its vector alone -- it is
    /// transient and only ever grows -- but a reset that kept a previous game's sky
    /// would make the next weather command's first frame a leftover.
    /// </remarks>
    public void Clear() => _particles.Clear();

    /// <summary>Rain: down four and left one per frame.</summary>
    private void UpdateRain()
    {
        for (var index = 0; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            if (particle.T > 0)
            {
                particle.T -= 1;
                particle.Y += 4;
                particle.X -= 1;
                _particles[index] = particle;
            }
            else if (PercentChance(10))
            {
                particle.T = 12;
                particle.X = _random.NextInclusive(0, PanLimitTilesX - 1);
                particle.Y = _random.NextInclusive(0, PanLimitTilesY - 1);
                _particles[index] = particle;
            }
        }
    }

    /// <summary>Snow: a random column left and two or three rows down.</summary>
    private void UpdateSnow()
    {
        for (var index = 0; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            if (particle.T > 0)
            {
                particle.T -= 1;
                particle.X -= _random.NextInclusive(0, 1);
                particle.Y += _random.NextInclusive(2, 3);
                _particles[index] = particle;
            }
            else if (PercentChance(5))
            {
                particle.T = 30;
                particle.X = _random.NextInclusive(0, PanLimitTilesX - 1);
                particle.Y = _random.NextInclusive(0, PanLimitTilesY - 1);
                _particles[index] = particle;
            }
        }
    }

    /// <summary>Fog: the two layers only advance.</summary>
    private void UpdateFog()
    {
        if (_particles.Count < FogParticles)
        {
            return;
        }
        for (var index = 0; index < FogParticles; index++)
        {
            var particle = _particles[index];
            particle.X += 1;
            _particles[index] = particle;
        }
    }

    /// <summary>Whether a chance of <paramref name="pPercent"/> in a hundred came up.</summary>
    /// <param name="pPercent">The chance, in percent.</param>
    /// <returns>Whether it happened.</returns>
    private bool PercentChance(int pPercent) => _random.NextInclusive(0, 99) < pPercent;

    /// <summary>Sandstorm: the fog layers, then every particle from index two.</summary>
    private void UpdateSandstorm()
    {
        UpdateFog();
        for (var index = FogParticles; index < _particles.Count; index++)
        {
            var particle = _particles[index];
            if (particle.T > 0)
            {
                particle.T -= 1;
                particle.Alpha += 2;
                particle.X += (int)particle.Vx;
                particle.Y += (int)particle.Vy;
                particle.Vx += particle.Ax;
                particle.Vy += particle.Ay;
                _particles[index] = particle;
            }
            else if (PercentChance(10))
            {
                particle.T = 80;
                // **And the angle is RPG_RT's own**: the reference's own comment says
                // the engine takes a number in [1, 127] and maps [0, 256) onto
                // [0, 2 * PI), and its epsilon exists to imitate exactly that.
                var angle = _random.NextInclusive(1, 127) * 2.0 * Math.PI / 256.0;
                var distance = _random.NextInclusive(16, 95);
                var cosine = Math.Cos(angle);
                var sine = Math.Sin(angle);
                particle.X = (int)(distance * cosine * 2.0) * 320 / 320 + 320 / 2;
                particle.Y = (int)(distance * sine) * 240 / 240;
                particle.Alpha = 180;
                particle.Vx = 0.0;
                particle.Vy = 0.0;
                particle.Ax = cosine * 2.0;
                particle.Ay = sine * 2.0;
                _particles[index] = particle;
            }
        }
    }
}
