using System;
using System.Collections.Generic;
using System.Linq;

namespace AuralObservatory.Core;

/// <summary>Preregistered trials: generated before data collection; no audio playback or clinical claims.</summary>
public enum ExperimentCondition { Active, Control, SilentCatch }
public sealed record ExperimentArm(string Id, string AlgorithmId, string StimulusSha256, double DigitalCeiling);
public sealed record ExperimentProtocol(
    int SchemaVersion, string ExperimentId, string PrimaryOutcome, string PrimaryContrast,
    int RandomSeed, int Pairs, int StimulusDurationSeconds, int PostStimulusSeconds,
    int WashoutSeconds, ExperimentArm Active, ExperimentArm Control,
    bool AutomaticallyAdjustGain = false, string? FrozenProtocolSha256 = null);
public sealed record PlannedTrial(int Sequence, int Pair, ExperimentCondition Condition, string ArmId, bool BlindedCodeProvided);

/// <summary>
/// Conservative N-of-1 design primitive: blocked AB/BA allocation, deterministic public audit,
/// with immutable pre-trial plan. Adaptive exploration must happen before this protocol is frozen.
/// </summary>
public static class ExperimentPlanner
{
    public static IReadOnlyList<PlannedTrial> Build(ExperimentProtocol p)
    {
        Validate(p);
        uint state = unchecked((uint)p.RandomSeed);
        if(state==0)state=0xAEDF51u;
        var result = new List<PlannedTrial>(p.Pairs*2);
        for(int pair=1;pair<=p.Pairs;pair++)
        {
            state ^= state << 13;state ^= state >> 17;state ^= state << 5;
            bool activeFirst = (state & 1u) == 1u;
            var first=activeFirst ? ExperimentCondition.Active : ExperimentCondition.Control;
            var second=activeFirst ? ExperimentCondition.Control : ExperimentCondition.Active;
            result.Add(new PlannedTrial(result.Count+1,pair,first,
                first==ExperimentCondition.Active?p.Active.Id:p.Control.Id,false));
            result.Add(new PlannedTrial(result.Count+1,pair,second,
                second==ExperimentCondition.Active?p.Active.Id:p.Control.Id,false));
        }
        return result;
    }
    public static void Validate(ExperimentProtocol p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if(p.SchemaVersion!=1)throw new NotSupportedException("Unknown experiment protocol version");
        if(string.IsNullOrWhiteSpace(p.ExperimentId)||p.ExperimentId.Length>80)
            throw new ArgumentException("Invalid experiment ID");
        if(p.PrimaryOutcome is not ("poststimulus_loudness_0_10" or "poststimulus_distress_0_10"))
            throw new ArgumentException("A single declared primary outcome is required");
        if(p.PrimaryContrast!="active_minus_control")throw new ArgumentException("Unknown contrast");
        if(p.Pairs is <2 or >120)throw new ArgumentOutOfRangeException(nameof(p.Pairs));
        if(p.StimulusDurationSeconds is <5 or >60 || p.PostStimulusSeconds is <10 or >180)
            throw new ArgumentOutOfRangeException(nameof(p),"Session timing outside preapproved bounds");
        if(p.WashoutSeconds is <30 or >86400)throw new ArgumentOutOfRangeException(nameof(p.WashoutSeconds));
        if(p.AutomaticallyAdjustGain)throw new ArgumentException("Adaptive gain is prohibited in confirmatory mode");
        if(p.Active is null||p.Control is null||p.Active.Id==p.Control.Id)
            throw new ArgumentException("Distinct active and control arms required");
        foreach(var arm in new[]{p.Active,p.Control})
        {
            if(string.IsNullOrWhiteSpace(arm.Id)||string.IsNullOrWhiteSpace(arm.AlgorithmId))
                throw new ArgumentException("Missing arm identity or algorithm provenance");
            if(string.IsNullOrWhiteSpace(arm.StimulusSha256) || arm.StimulusSha256.Length!=64 || !arm.StimulusSha256.All(Uri.IsHexDigit))
                throw new ArgumentException("Each arm needs a hex SHA-256 of its exact stimulus");
            if(!double.IsFinite(arm.DigitalCeiling)||arm.DigitalCeiling<0||arm.DigitalCeiling>StimulusValidation.DigitalPeakCeiling)
                throw new ArgumentOutOfRangeException(nameof(arm.DigitalCeiling),"Digital ceiling too high");
        }
        if(p.FrozenProtocolSha256 is {Length: >0} hash && (hash.Length!=64 || !hash.All(Uri.IsHexDigit)))
            throw new ArgumentException("Malformed frozen protocol SHA-256");
    }
}
