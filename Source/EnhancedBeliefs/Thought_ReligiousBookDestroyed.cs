namespace EnhancedBeliefs;

public class Thought_ReligiousBookDestroyed : Thought_Memory
{
    private Ideo? destroyedBookIdeo;

    public Ideo? DestroyedBookIdeo
    {
        get => destroyedBookIdeo;
        set => destroyedBookIdeo = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref destroyedBookIdeo, "destroyedBookIdeo");
    }
}
