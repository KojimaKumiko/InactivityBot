using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InactivityBot.Interfaces
{
    public interface ISaveable
    {
        public Task LoadJsonAsync(string fileName);

        public Task SaveJsonAsync(string fileName);
    }
}
