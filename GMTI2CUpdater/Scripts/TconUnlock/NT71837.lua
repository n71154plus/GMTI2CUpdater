return {
    name = "Novatek NT71837",
    unlock = function(ctx, deviceAddress)
        ctx:WriteDpcd(0x0102, {0x00})
        ctx:WriteI2CByteIndex(0xC8, 0x10, {0x01})
        ctx:WriteI2CByteIndex(0xC8, 0x29, {0x00})
        ctx:WriteI2CByteIndex(0xC8, 0x04, {0x80})
        ctx:WriteDpcd(0x0102, {0xC0})
    end,
    lock = function(ctx, deviceAddress)
    end
}
