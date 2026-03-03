// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationSteps.cs" company="MLambda">
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MLambda.Actors.Network.Test.Steps
{
    using System;
    using MLambda.Actors.Network.Abstraction;
    using MLambda.Actors.Network.Test.Messages;
    using Reqnroll;
    using Shouldly;

    /// <summary>
    /// Step definitions for serialization feature tests.
    /// </summary>
    [Binding]
    public class SerializationSteps
    {
        private readonly ScenarioContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationSteps"/> class.
        /// </summary>
        /// <param name="context">The scenario context.</param>
        public SerializationSteps(ScenarioContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates a JsonMessageSerializer.
        /// </summary>
        [Given(@"a JsonMessageSerializer")]
        public void GivenAJsonMessageSerializer()
        {
            this.context["serializer"] = new JsonMessageSerializer();
        }

        /// <summary>
        /// Serializes a string message.
        /// </summary>
        /// <param name="message">The string message.</param>
        [When(@"I serialize the string message ""(.*)""")]
        public void WhenISerializeTheStringMessage(string message)
        {
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");
            var bytes = serializer.Serialize(message);
            var typeName = serializer.GetTypeName(message);
            var result = serializer.Deserialize(bytes, typeName);
            this.context["deserialized_string"] = result;
        }

        /// <summary>
        /// Verifies the deserialized string message.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        [Then(@"the deserialized message should equal ""(.*)""")]
        public void ThenTheDeserializedMessageShouldEqual(string expected)
        {
            var result = this.context.Get<object>("deserialized_string");
            result.ToString().ShouldBe(expected);
        }

        /// <summary>
        /// Serializes an integer message.
        /// </summary>
        /// <param name="value">The integer value.</param>
        [When(@"I serialize the integer message (\d+)")]
        public void WhenISerializeTheIntegerMessage(int value)
        {
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");
            var bytes = serializer.Serialize(value);
            var typeName = serializer.GetTypeName(value);
            var result = serializer.Deserialize(bytes, typeName);
            this.context["deserialized_int"] = result;
        }

        /// <summary>
        /// Verifies the deserialized integer message.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        [Then(@"the deserialized integer message should equal (\d+)")]
        public void ThenTheDeserializedIntegerMessageShouldEqual(int expected)
        {
            var result = this.context.Get<object>("deserialized_int");
            Convert.ToInt32(result).ShouldBe(expected);
        }

        /// <summary>
        /// Serializes a complex message.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        [When(@"I serialize a complex message with Name ""(.*)"" and Value (\d+)")]
        public void WhenISerializeAComplexMessage(string name, int value)
        {
            var serializer = this.context.Get<JsonMessageSerializer>("serializer");
            var msg = new TestMessage { Name = name, Value = value };
            var bytes = serializer.Serialize(msg);
            var typeName = serializer.GetTypeName(msg);
            var result = (TestMessage)serializer.Deserialize(bytes, typeName);
            this.context["deserialized_complex"] = result;
        }

        /// <summary>
        /// Verifies the deserialized complex message.
        /// </summary>
        /// <param name="name">The expected name.</param>
        /// <param name="value">The expected value.</param>
        [Then(@"the deserialized complex message should have Name ""(.*)"" and Value (\d+)")]
        public void ThenTheDeserializedComplexMessageShouldHave(string name, int value)
        {
            var result = this.context.Get<TestMessage>("deserialized_complex");
            result.Name.ShouldBe(name);
            result.Value.ShouldBe(value);
        }

        /// <summary>
        /// Creates an envelope with the given type.
        /// </summary>
        /// <param name="typeName">The envelope type name.</param>
        [Given(@"an envelope with type ""(.*)"" and correlation id")]
        public void GivenAnEnvelopeWithTypeAndCorrelationId(string typeName)
        {
            var type = Enum.Parse<EnvelopeType>(typeName);
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = Guid.NewGuid(),
                SourceActorId = Guid.NewGuid(),
                SourceNode = new NodeEndpoint("127.0.0.1", 9000),
                Type = type,
                PayloadTypeName = "System.String",
                PayloadBytes = new byte[] { 1, 2, 3 },
            };
            this.context["original_envelope"] = envelope;
        }

        /// <summary>
        /// Encodes and decodes the envelope.
        /// </summary>
        [When(@"the envelope is encoded and decoded")]
        public void WhenTheEnvelopeIsEncodedAndDecoded()
        {
            var original = this.context.Get<Envelope>("original_envelope");
            var encoded = EnvelopeCodec.Encode(original);
            var decoded = EnvelopeCodec.Decode(encoded);
            this.context["decoded_envelope"] = decoded;
        }

        /// <summary>
        /// Verifies the decoded envelope matches the original.
        /// </summary>
        [Then(@"the decoded envelope should match the original")]
        public void ThenTheDecodedEnvelopeShouldMatchTheOriginal()
        {
            var original = this.context.Get<Envelope>("original_envelope");
            var decoded = this.context.Get<Envelope>("decoded_envelope");
            decoded.CorrelationId.ShouldBe(original.CorrelationId);
            decoded.TargetActorId.ShouldBe(original.TargetActorId);
            decoded.SourceActorId.ShouldBe(original.SourceActorId);
            decoded.SourceNode.NodeId.ShouldBe(original.SourceNode.NodeId);
            decoded.Type.ShouldBe(original.Type);
            decoded.PayloadTypeName.ShouldBe(original.PayloadTypeName);
            decoded.PayloadBytes.ShouldBe(original.PayloadBytes);
        }

        /// <summary>
        /// Creates an ask envelope with source node.
        /// </summary>
        /// <param name="typeName">The envelope type name.</param>
        /// <param name="host">The source host.</param>
        /// <param name="port">The source port.</param>
        [Given(@"an envelope with type ""(.*)"" and source node ""(.*)"" port (\d+)")]
        public void GivenAnEnvelopeWithTypeAndSourceNode(string typeName, string host, int port)
        {
            var type = Enum.Parse<EnvelopeType>(typeName);
            var envelope = new Envelope
            {
                CorrelationId = Guid.NewGuid(),
                TargetActorId = Guid.NewGuid(),
                SourceActorId = Guid.NewGuid(),
                SourceNode = new NodeEndpoint(host, port),
                Type = type,
                PayloadTypeName = "System.Int32",
                PayloadBytes = new byte[] { 42 },
            };
            this.context["original_envelope"] = envelope;
        }

        /// <summary>
        /// Verifies the decoded envelope type.
        /// </summary>
        /// <param name="expectedType">The expected type.</param>
        [Then(@"the decoded envelope should have type ""(.*)""")]
        public void ThenTheDecodedEnvelopeShouldHaveType(string expectedType)
        {
            var decoded = this.context.Get<Envelope>("decoded_envelope");
            decoded.Type.ShouldBe(Enum.Parse<EnvelopeType>(expectedType));
        }

        /// <summary>
        /// Verifies the decoded envelope source node.
        /// </summary>
        /// <param name="host">The expected host.</param>
        /// <param name="port">The expected port.</param>
        [Then(@"the decoded envelope source node should be ""(.*)"" port (\d+)")]
        public void ThenTheDecodedEnvelopeSourceNodeShouldBe(string host, int port)
        {
            var decoded = this.context.Get<Envelope>("decoded_envelope");
            decoded.SourceNode.NodeId.ShouldBe(host);
            decoded.SourceNode.Port.ShouldBe(port);
        }
    }
}
