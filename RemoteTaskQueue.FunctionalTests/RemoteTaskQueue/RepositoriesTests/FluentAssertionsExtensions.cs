﻿using System;
using System.Collections.Generic;

using FluentAssertions;
using FluentAssertions.Equivalency;

namespace RemoteTaskQueue.FunctionalTests.RemoteTaskQueue.RepositoriesTests
{
    public static class FluentAssertionsExtensions
    {
        public static void ShouldBeEquivalentWithOrderTo<T>(this IEnumerable<T> actual, params T[] expected)
        {
            actual.ShouldBeEquivalentWithOrderTo((IEnumerable<T>)expected);
        }

        public static void ShouldBeEquivalentWithOrderTo<T>(this IEnumerable<T> actual, IEnumerable<T> expected, Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>> config = null, string because = "", params object[] becauseArgs)
        {
            config = config ?? (opt => opt);
            actual.ShouldAllBeEquivalentTo(expected, opt => config(opt).WithStrictOrderingFor(x => x), because, becauseArgs);
        }
    }
}