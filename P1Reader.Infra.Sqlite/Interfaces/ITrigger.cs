namespace P1ReaderApp.Interfaces
{
    public delegate void TriggerEventHandler<TArgs>(
        TArgs args);

    public interface ITrigger<TArgs>
    {
        void FireTrigger(
            TArgs arg);

        event TriggerEventHandler<TArgs> Trigger;
    }
}
