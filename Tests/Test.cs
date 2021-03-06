using System;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    public abstract class Test
    {
        [TestFixtureSetUp]
        public virtual void Init ()
        {
        }

        [TestFixtureTearDown]
        public virtual void Cleanup ()
        {
        }

        [SetUp]
        public virtual void SetUp ()
        {
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<ModelManager> ();
        }

        [TearDown]
        public virtual void TearDown ()
        {
            ServiceContainer.Clear ();
        }

        protected MessageBus MessageBus {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }
    }
}
