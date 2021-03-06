
## 应用场景能耗测试

Unity2021，分别单独开启以下测试用例，使得手机满帧60却只能跑到30左右


![](Doc/scenario_test_performance_data.png)

- CPU计算：分配核心数个工作线程，计算足够多的平方根
- CPU读写：`Buffer.MemoryCopy`ping-ping不同内容足够多量
- GPU计算：unroll足够多的`half mad`操作
- GPU读写：两张4K float RenderTarget，ping-pong resolve不同内容足够多次

## 应用场景能耗比较

多次实验

|机型|测试用例|平均帧率$(帧/s)$|平均功率$(mW)$|每帧能耗$(mJ/帧)$
|--|--|--|--|--|
iPhone 12 PM|未开启测试|	59|	1490.3|	25.26
iPhone 12 PM|CPU计算|	30.1	|4528	|150.43
iPhone 12 PM|CPU读写|	29.4	|4628.7	|157.97
iPhone 12 PM|GPU计算	|27.4	|2996.8	|109.37
iPhone 12 PM|GPU读写|	28.8|	4303.8|	149.44
||
iPhone 6s|未开启测试|	58.8|	1256|	21.36
iPhone 6s|CPU计算| 28	|4459.1	|159.25
iPhone 6s|CPU读写|	29.1	|4179.9	|143.64
iPhone 6s|GPU计算	|24.8	|3133|126.33
iPhone 6s|GPU读写|24.9|	4222.5|	169.58



**按游戏应用场景划分功耗**，**CPU计算$\approx$CPU读写$\approx$GPU读写$>$GPU计算$>>$内存存储$^{*}$**


## 测试环境

![](Doc/testing_scene_ice_and_fire.png)

- Perfdog无线测能耗模式
- 冰冷风冷散热，避免降频影响测试结果
- 适当降低屏幕亮度，减少引起噪音

## 多次单盲AB对比帧能耗测试

1. 构建出有版本号的两个版本，分别是版本A和B；
2. 确保两个版本不包含别的修改，只包含需测试的特性修改；
3. （可选）测试者不知道且猜不出A和B的区别；
4. 测试者运行相同的测试用例；
5. 记录版本号和测试数据：能耗，**并除以总帧数**
6. 重复以上步骤多次，直到记录测量结果的最小最大值稳定；
7. 对比版本A、A的测量结果，分析结果的最小、最大、中位和平均值，
8. 对比版本A、B的测量结果，分析结果的最小、最大、中位和平均值，
9. 分析数据，AB的信噪比是否大于AA信噪比，是否符合优化期望

