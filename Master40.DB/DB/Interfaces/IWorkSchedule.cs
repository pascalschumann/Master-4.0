﻿
namespace Master40.DB.Models.Interfaces
{
    public interface IWorkSchedule
    {
        int HierarchyNumber { get; set; }
        string Name { get; set; }
        int Duration { get; set; }
        int MachineId { get; set; }
    }
}
