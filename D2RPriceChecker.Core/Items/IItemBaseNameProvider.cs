using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Items
{
    public interface IItemBaseNameProvider
    {
        IReadOnlyList<BaseNameEntry> GetAllBaseNames();
    }
}
