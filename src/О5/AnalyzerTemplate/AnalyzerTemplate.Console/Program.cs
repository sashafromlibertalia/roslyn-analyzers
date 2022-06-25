namespace AnalyzerTemplate.Console
{
    internal class Student
    {
        private string _name;
        public Student(string name)
        {
            _name = name;
        }
    }

    internal static class Test
    {
        public static Student TryGetValue()
        {
            int val = 5;
            Random rd = new Random();
            int num = rd.Next(1,5);

            if (num == 1)
            {
                System.Console.WriteLine();
                val = 5;
                return new Student("Вася");
            }

            if (num == 2) {
                return new Student("Коля");
            }
            return new Student("Петя");
        }

        private static void Main(string[] args)
        {
        }
    }
}