using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DOTS
{
    public struct StateGroup : IDisposable, IEnumerable<State>
    {
        private NativeList<State> _states;

        public StateGroup(int initialCapacity, Allocator allocator)
        {
            _states = new NativeList<State>(
                initialCapacity, allocator);
        }

        public StateGroup(StateGroup copyFrom, Allocator allocator)
        {
            _states = new NativeList<State>(copyFrom.Length(), allocator);
            foreach (var state in copyFrom._states)
            {
                _states.Add(state);
            }
        }
        
        public StateGroup([ReadOnly]ref DynamicBuffer<State> buffer, Allocator allocator)
        {
            _states = new NativeList<State>(buffer.Length, allocator);
            foreach (var state in buffer)
            {
                _states.Add(state);
            }
        }

        public State this[int key]
        {
            get => _states[key];
            set => _states[key] = value;
        }

        public int Length()
        {
            return _states.Length;
        }

        public void Dispose()
        {
            _states.Dispose();
        }

        public void Add(State state)
        {
            _states.Add(state);
        }

        /// <summary>
        /// 相同项则无视，不同项则增加
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public void Merge(StateGroup other)
        {
            foreach (var otherState in other._states)
            {
                var contained = false;
                foreach (var state in _states)
                {
                    if (state.Equals(otherState))
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained) _states.Add(otherState);
            }
        }
        
        /// <summary>
        /// 相同项则移除，不同项则无视
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public void Sub(StateGroup other)
        {
            foreach (var otherState in other._states)
            {
                for (var i = _states.Length-1; i >= 0; i--)
                {
                    var state = _states[i];
                    if (state.Equals(otherState))
                    {
                        _states.RemoveAtSwapBack(i);
                    }
                }
            }
        }

        public void WriteBuffer(ref DynamicBuffer<State> buffer)
        {
            foreach (var state in _states)
            {
                buffer.Add(state);
            }
        }

        public IEnumerator<State> GetEnumerator()
        {
            return _states.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}