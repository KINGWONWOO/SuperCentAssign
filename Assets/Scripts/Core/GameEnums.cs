public enum ItemType { Ore, Handcuff, Cash }

public enum ToolLevel { Pickaxe = 0, Drill = 1, DrillCar = 2 }

public enum PrisonerState
{
    WalkingToWaitPos,
    WaitingBehindDesk,
    AtDesk,
    FullyProcessed,
    WalkingToPrisonWait,  // desk 처리 후 감옥 입구 경유
    WalkingToCell,
    InCell,
    WaitingOutsideCell
}

public enum DropZoneType { OreToConverter, HandcuffPickup, MoneyPickup }

public enum UpgradeType { Drill, DrillCar, WorkerHire, AutoSell, PrisonExpand }
