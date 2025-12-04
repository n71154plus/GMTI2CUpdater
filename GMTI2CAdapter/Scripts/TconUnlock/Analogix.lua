return {
    name = "Analogix Common",
    unlock = function(ctx, deviceAddress)
        ctx:WriteDpcd(0x04F5, {0x41, 0x56, 0x4F, 0x20, 0x16})
        ctx:WriteDpcd(0x04F0, {0x0E, 0x00, 0x00, 0x30, 0x09})
    end,
    lock = function(ctx, deviceAddress)
    end
}
