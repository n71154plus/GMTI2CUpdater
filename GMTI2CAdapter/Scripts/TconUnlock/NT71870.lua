return {
    name = "Novatek NT71870",
    unlock = function(ctx, deviceAddress)
        ctx:WriteDpcd(0x0102, {0xC0})
        ctx:WriteDpcd(0x048B, {0x18})
        ctx:WriteI2CUInt16Index(0xC0, 0x0A26, {0xC1})
    end,
    lock = function(ctx, deviceAddress)
        ctx:WriteI2CUInt16Index(0xC0, 0x0A26, {0xC1})
        ctx:WriteDpcd(0x048B, {0x00})
        ctx:WriteDpcd(0x0102, {0x81})
    end
}
