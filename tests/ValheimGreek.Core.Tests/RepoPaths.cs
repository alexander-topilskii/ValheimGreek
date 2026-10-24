using System;
using System.IO;

namespace ValheimGreek.Core.Tests
{
    internal static class RepoPaths
    {
        public static string Greek3Words()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.Parent != null)
                {
                    var sibling = Path.Combine(dir.Parent.FullName, "Greek3", "words");
                    if (Directory.Exists(sibling))
                        return sibling;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Greek3/words was not found above " + AppContext.BaseDirectory);
        }
    }
}
