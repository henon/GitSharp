using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace GitSharp.TestRunner
{
    internal class TestFixtureView
    {
        public TestFixtureView(string name, Type fixture)
        {
            Name = name;
            FixtureType = fixture;
            Tests = new List<TestcaseView>();
        }

        public string Name { get; set; }
        public Type FixtureType { get; set; }
        public MethodInfo SetupMethod { get; set; }
        public MethodInfo TeardownMethod { get; set; }
        public List<TestcaseView> Tests { get; private set; }

        internal void AddTest(TestcaseView testcase)
        {
            testcase.Fixture = this;
            Tests.Add(testcase);
        }

        private object m_instance;
        public object Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = CreateInstance(FixtureType, new object[0]);
                }
                return m_instance;
            }
        }

        public static object CreateInstance(Type type, params object[] args)
        {
            Debug.Assert(type != null, "type cannot be null!!!!");
            try
            {
                return type.InvokeMember(
                    null,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.CreateInstance |
                    BindingFlags.Instance,
                    null,
                    null,
                    args);
            }
            catch (TargetInvocationException e)
            {
                Console.WriteLine(e.PrettyPrint());
                throw e;
            }
        }
    }
}
