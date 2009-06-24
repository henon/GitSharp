using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace GitSharp.TestRunner
{
    internal class TestcaseView
    {
        public TestcaseView(Type t, MethodInfo m)
        {
            Type = t;
            TestMethod = m;
        }

        public Type Type
        {
            get;
            set;
        }

        public MethodInfo TestMethod
        {
            get;
            set;
        }


        public TestFixtureView Fixture { get; set; }

        public void Setup()
        {
            if (Fixture.SetupMethod == null)
                return;
            Fixture.SetupMethod.Invoke(Fixture.Instance, new object[0]);
        }

        public void Teardown()
        {
            if (Fixture.TeardownMethod == null)
                return;
            try
            {
                Fixture.TeardownMethod.Invoke(Fixture.Instance, new object[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException.PrettyPrint());
                //Exception = e.InnerException;
            }
        }

        public Exception Exception
        {
            get;
            internal set;
        }

        public bool Failed
        {
            get { return Exception != null; }
        }

        public bool IsExecuted { get; set; }

        public void Run()
        {
            IsExecuted = true;
            Exception = null;
            try
            {
                Setup();
                TestMethod.Invoke(Fixture.Instance, new object[0]);
            }
            catch (Exception e)
            {
                Exception = e.InnerException;
            }
            finally
            {
                Teardown();
            }
        }

        public override string ToString()
        {
            return Type.Name + "." + TestMethod.Name;
        }
    }
}
