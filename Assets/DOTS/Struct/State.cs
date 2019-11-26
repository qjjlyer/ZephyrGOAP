using System;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public StateSubjectType SubjectType;
        public ComponentType Trait;
        public NativeString64 Value;
        public bool IsPositive;
        
        public Entity Target;

        public override int GetHashCode()
        {
            return Target.GetHashCode() + SubjectType.GetHashCode() + Trait.GetHashCode() +
                   Value.GetHashCode() + IsPositive.GetHashCode();
        }

        public bool Equals(State other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// 相比Equals，Fits范围更宽
        /// 非指定Entity的Closest与指定Entity之间也算Fit
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Fits(State other)
        {
            if (Equals(other)) return true;

            //这三项必须一致
            if (!(Trait.Equals(other.Trait) && Value.Equals(other.Value) &&
                  IsPositive.Equals(IsPositive)))
            {
                return false;
            }

            //我与对方任何一个为Closest另一个为指定Entity则算作Fit
            if (SubjectType == StateSubjectType.Closest && other.Target != Entity.Null) return true;
            if (Target != Entity.Null && other.SubjectType == StateSubjectType.Closest) return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is State other && Equals(other);
        }
    }
}