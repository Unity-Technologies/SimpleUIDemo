using NUnit.Framework;
using System;
using UnityEngine;

namespace Unity.Serialization.Json.Tests
{
    partial class JsonSerializationTests
    {
        class ClassContainer : IEquatable<ClassContainer>
        {
            public bool Equals(ClassContainer other)
            {
                return other != null;
            }
        }

        class ContainerWithClassContainerFields
        {
            public string String;
            public ClassContainer Value;
            public ClassContainer[] Array;
            public ClassContainer[] MixedArray;
        }

        [Test]
        public void JsonSerialization_Serialize_Null()
        {
            var src = new ContainerWithClassContainerFields
            {
                String = null,
                Value = null,
                Array = new ClassContainer[3],
                MixedArray = new[] { null, new ClassContainer(), null, new ClassContainer(), new ClassContainer(), null }
            };
            var json = JsonSerialization.Serialize(src);
            Debug.Log(json);
            Assert.That(json, Is.EqualTo("{\n    \"String\": null,\n    \"Value\": null,\n    \"Array\": [\n        null,\n        null,\n        null\n    ],\n    \"MixedArray\": [\n        null, {},\n        null, {}, {},\n        null\n    ]\n}"));

            var dst = new ContainerWithClassContainerFields();
            using (JsonSerialization.DeserializeFromString(json, ref dst))
            {
                Assert.That(dst.String, Is.Null);
                Assert.That(dst.Value, Is.Null);
                Assert.That(dst.Array, Is.EqualTo(new ClassContainer[3]));
                Assert.That(dst.MixedArray, Is.EqualTo(new[] { null, new ClassContainer(), null, new ClassContainer(), new ClassContainer(), null }));
            }
        }
    }
}
