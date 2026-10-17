namespace Practice.UI
{
    /// <summary>
    /// Contract for a scene-local demo action that the <see cref="PracticeDemoHub"/> exposes as a button.
    /// Implemented by scene components whose behaviour is worth demonstrating to a reviewer.
    /// </summary>
    public interface IDemoAction
    {
        /// <summary>Label shown on the generated button.</summary>
        string ButtonLabel { get; }

        /// <summary>Invoked when the generated button is clicked.</summary>
        void Execute();
    }
}
