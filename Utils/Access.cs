using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorreWithCare.Utils;

public static class Access
{
    public static CorreWithCareModule Module => CorreWithCareModule.Instance;
    public static CorreWithCareSession Session => CorreWithCareModule.Session;
    public static CorreWithCareSettings Settings => CorreWithCareModule.Settings;
    public static CorreWithCareSaveData SaveData => CorreWithCareModule.SaveData;
}
