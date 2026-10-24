namespace ValheimGreek
{
    public static class GameUi
    {
        public static bool BlocksQuiz()
        {
            if (Chat.instance != null && Chat.instance.HasFocus())
                return true;
            if (global::Console.IsVisible())
                return true;
            if (global::Menu.IsVisible())
                return true;
            if (global::TextInput.IsVisible())
                return true;
            if (InventoryGui.IsVisible())
                return true;
            return false;
        }
    }
}
