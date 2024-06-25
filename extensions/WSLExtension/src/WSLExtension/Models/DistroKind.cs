// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WSLExtension.Models;

public class DistroKind : IDistroKind
{
    public DistroKind(IDistroKind other)
    {
        Name = other.Name;
        Logo = other.Logo;
        IdLike = other.IdLike.ToList();
    }

    public static IEqualityComparer<DistroKind> NameComparer { get; } = new NameEqualityComparer();

    public int CompareTo(IDistroKind? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (ReferenceEquals(null, other))
        {
            return 1;
        }

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    public string Name { get; set; }

    public string Logo { get; set; }

    public List<string> IdLike { get; set; }

    protected bool Equals(IDistroKind? other)
    {
        return other != null && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals((IDistroKind)obj);
    }

    public override int GetHashCode()
    {
        return Name != null ? Name.GetHashCode() : 0;
    }

    private sealed class NameEqualityComparer : IEqualityComparer<IDistroKind>
    {
        public bool Equals(IDistroKind? x, IDistroKind? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Name == y.Name;
        }

        public int GetHashCode(IDistroKind obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    private sealed class NameRelationalComparer : IComparer<IDistroKind>
    {
        public int Compare(IDistroKind? x, IDistroKind? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }

    public static bool operator ==(DistroKind? left, DistroKind? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(DistroKind left, DistroKind right)
    {
        return !(left == right);
    }

    public static bool operator <(DistroKind left, DistroKind right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(DistroKind left, DistroKind right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(DistroKind left, DistroKind right)
    {
        var result = left.CompareTo(right);

        return result is < 0 or 0;
    }

    public static bool operator >=(DistroKind left, DistroKind right)
    {
        var result = left.CompareTo(right);

        return result is > 0 or 0;
    }
}
