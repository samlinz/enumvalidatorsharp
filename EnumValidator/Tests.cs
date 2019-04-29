using System;
using System.Reflection;
using Xunit;

namespace EnumReader {
    public class Tests {
        public enum TestEnum1 {
            Unknown = 0,
            First = 1,
            Second = 1,
            Third = 3
        }
        
        public enum TestEnum2 {
            Unknown = 0,
            First = 1,
            Second = 2,
            Third = 333,
            Fourth = 333,
        }
        
        [Fact]
        public void ShouldLogDuplicates() {
            var validator = new EnumValidator(Console.WriteLine, s => Console.WriteLine("error " + s));
            validator.SetLoadedAssembly(Assembly.GetExecutingAssembly());

            int loggedDuplicates = 0;
            
            validator.LogEnumDuplicates += tuples => {
                (int value, string enumValues) valueTuple = tuples[0];

                switch (valueTuple.value) {
                    case 1:
                        Assert.Equal(1, valueTuple.value);
                        Assert.Matches("First", valueTuple.enumValues);
                        Assert.Matches("Second", valueTuple.enumValues);
                        loggedDuplicates++;
                        break;
                    case 333:
                        Assert.Equal(333, valueTuple.value);
                        Assert.Matches("Third", valueTuple.enumValues);
                        Assert.Matches("Fourth", valueTuple.enumValues);
                        loggedDuplicates++;
                        break;
                }
            };
            validator.Run();
            
            Assert.Equal(2, loggedDuplicates);
        }
    }
}