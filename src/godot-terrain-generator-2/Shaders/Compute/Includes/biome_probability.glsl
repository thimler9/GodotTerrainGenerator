float biome_depth_probability(float sample_depth, float biome_depth, float depth_spread) {
    if (depth_spread == 0.0) {
        return sample_depth == biome_depth ? 1.0 : 0.0;
    }

    float depth_difference = sample_depth - biome_depth;
    return exp(-(depth_difference * depth_difference) / (depth_spread * depth_spread));
}
