using System;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AssaultCube
{
    // Estructura Player
    [StructLayout(LayoutKind.Sequential)]
    public struct Player
    {
        public IntPtr entityObj;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string playerName;
        public Vector3 pos;
        public int health;
        public int teamID;
        public float distance;

        public Player(IntPtr entityObj, string playerName, Vector3 pos, int health, int teamID, float distance)
        {
            this.entityObj = entityObj;
            this.playerName = playerName;
            this.pos = pos;
            this.health = health;
            this.teamID = teamID;
            this.distance = distance;
        }

        public override string ToString()
        {
            return $"Name: {playerName} | Pos: [X: {pos.X}, Y: {pos.Y}, Z: {pos.Z}]";
        }
    }

}
