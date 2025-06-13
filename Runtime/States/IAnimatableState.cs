namespace Behaviours.HFSM
{
    public interface IAnimatableState
    {
        string AnimationName { get; set; }

        int AnimationTrack  { get; }

        float AnimationSpeed { get; }

        float AnimationStart { get; }

        float AnimationEnd { get; }
         
        float AnimationDuration { get; }
    }
}