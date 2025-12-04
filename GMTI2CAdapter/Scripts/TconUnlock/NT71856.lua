return {
    name = "Novatek NT71856",
    unlock = function(ctx, deviceAddress)
        local mode = ctx:ReadDpcd(0x04C1, 1)
        mode[1] = bit32.band(bit32.bor(mode[1], 0x04), 0xF7)
        ctx:WriteDpcd(0x04C1, mode)
        ctx:WriteDpcd(0x0102, {0xC0})
        local reg0204 = ctx:ReadI2CUInt16Index(0xC0, 0x0204, 1)
        reg0204[1] = bit32.band(reg0204[1], 0xF0)
        ctx:WriteI2CUInt16Index(0xC0, 0x0204, reg0204)
    end,
    lock = function(ctx, deviceAddress)
        local mode = ctx:ReadDpcd(0x04C1, 1)
        mode[1] = bit32.bor(mode[1], 0x0C)
        ctx:WriteDpcd(0x04C1, mode)
    end
}
