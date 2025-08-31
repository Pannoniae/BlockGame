using BlockGame.GL;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.src.render;

public abstract class DynamicTexture {
    public BTextureAtlas parent;
    
    public readonly int atlasX, atlasY;
    public readonly int width, height;
    
    protected Rgba32[] pixels;
    protected int tickCounter;
    protected bool needsUpdate;
    
    public abstract int updateFreq { get; }

    protected DynamicTexture(BTextureAtlas parent, int atlasX, int atlasY, int width, int height) {
        this.parent = parent;
        this.atlasX = atlasX;
        this.atlasY = atlasY;
        this.width = width;
        this.height = height;
        pixels = new Rgba32[width * height];
    }
    
    public void tick() {
        tickCounter++;
        if (tickCounter % updateFreq == 0) {
            update();
            if (needsUpdate) {
                parent?.updateTexture(atlasX, atlasY, width, height, pixels);
                needsUpdate = false;
            }
        }
    }
    
    protected abstract void update();
    
    protected void markDirty() {
        needsUpdate = true;
    }
}

public class StillWaterTexture(BTextureAtlas parent) : DynamicTexture(parent, 0, 4 * 16, 16, 16) {
    private int frameIndex;
    
    public override int updateFreq => 16;

    protected override void update() {
        if (parent?.imageData.IsEmpty == false) {
            int srcX = (frameIndex % 16) * 16;
            int srcY = 4 * 16;
            
            var span = parent.imageData.Span;
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    pixels[y * width + x] = span[(srcY + y) * parent.image.Width + srcX + x];
                }
            }
            frameIndex++;
            markDirty();
        }
    }
}

public class FlowingWaterTexture : DynamicTexture {
    private readonly Rgba32[] src;
    
    public override int updateFreq => 4;

    public FlowingWaterTexture(BTextureAtlas parent) : base(parent, 0, 8 * parent.atlasSize, 32, 32) {
        src = new Rgba32[width * height];
        var span = parent.imageData.Span;
        int srcY = 8 * parent.atlasSize;
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                src[y * width + x] = span[(srcY + y) * parent.image.Width + x];
            }
        }
    }

    protected override void update() {
        for (int y = 0; y < height; y++) {
            var lastPixel = src[y * width + width - 1];
            for (int x = width - 1; x > 0; x--) {
                src[y * width + x] = src[y * width + x - 1];
            }
            src[y * width] = lastPixel;
        }
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                pixels[y * width + x] = src[y * width + x];
            }
        }
        
        markDirty();
    }
}