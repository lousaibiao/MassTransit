// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Automatonymous.Activities
{
    using System;
    using System.Threading.Tasks;
    using GreenPipes;
    using MassTransit;


    public class RespondActivity<TInstance, TData, TMessage> :
        Activity<TInstance, TData>
        where TInstance : SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        readonly EventMessageFactory<TInstance, TData, TMessage> _messageFactory;
        readonly IPipe<SendContext<TMessage>> _responsePipe;

        public RespondActivity(EventMessageFactory<TInstance, TData, TMessage> messageFactory,
            Action<SendContext<TMessage>> contextCallback)
        {
            _messageFactory = messageFactory;

            _responsePipe = Pipe.Execute(contextCallback);
        }

        public RespondActivity(EventMessageFactory<TInstance, TData, TMessage> messageFactory)
        {
            _messageFactory = messageFactory;

            _responsePipe = Pipe.Empty<SendContext<TMessage>>();
        }

        void Visitable.Accept(StateMachineVisitor inspector)
        {
            inspector.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("respond");
        }

        async Task Activity<TInstance, TData>.Execute(BehaviorContext<TInstance, TData> context, Behavior<TInstance, TData> next)
        {
            ConsumeEventContext<TInstance, TData> consumeContext = context.CreateConsumeContext();

            var message = _messageFactory(consumeContext);

            await consumeContext.RespondAsync(message, _responsePipe).ConfigureAwait(false);

            await next.Execute(context).ConfigureAwait(false);
        }

        Task Activity<TInstance, TData>.Faulted<TException>(BehaviorExceptionContext<TInstance, TData, TException> context,
            Behavior<TInstance, TData> next)
        {
            return next.Faulted(context);
        }
    }
}