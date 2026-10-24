using ValheimGreek.Core;

namespace ValheimGreek
{
    public sealed class PendingAction
    {
        public ItemDrop? Item { get; set; }
        public bool Repeat { get; set; }
        public bool Alt { get; set; }
        public int Required { get; set; }
        public int Correct { get; set; }
        public QuizQuestion? Question { get; set; }
    }

    public static class InteractBypass
    {
        public static bool Active { get; private set; }

        public static bool Run(ItemDrop item, Humanoid character, bool repeat, bool alt)
        {
            Active = true;
            try
            {
                return item.Interact(character, repeat, alt);
            }
            finally
            {
                Active = false;
            }
        }
    }
}
