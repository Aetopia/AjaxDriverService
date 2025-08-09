using System.Collections.Generic;

sealed class Device
{
    readonly string _string;

    public readonly IEnumerable<Driver> Drivers;

    internal Device(string id, IEnumerable<Driver> drivers) => (_string, Drivers) = (id, drivers);

    public override string ToString() => _string;
}