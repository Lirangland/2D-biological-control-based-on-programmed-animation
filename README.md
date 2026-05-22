Unity版本：6000.3.3f1

核心组件及功能：

1.PointConstraintData

一个点（以下称为“当前点”）对于约束目标点的约束信息，可以设置与其他任意多个点之间的约束关系，包括最大距离、最小距离、固定距离、弹性回正、角度约束等，并可在编辑器视图中进行设置。

1.1属性及功能

Weight Type：权重类型，分为SelfWeight（自身权重）、TargetFullWeight（目标点完全权重）、SelfFullWeight（自身完全权重）三类。

Target Look At Self：勾选后，目标点的前方将一直朝向当前点。

Target：目标点。

Min Distance：最小距离，最小为0，超过最大距离时视为最大距离。

Max Distance：最大距离，为0时视为无限远。

Fixed Distance：固定距离，大于0时，无视最小和最大距离，采用固定距离。

Elasticity：弹性回正系数，为0或1时直接设置距离，否则按系数逐步回正。

Apply Angle Constraint：勾选后启用角度约束。

Angle Constraint Direction Type：角度约束参考方向，分为NextChainToTarget（下一约束段指向目标点，若没有下一约束段则不生效）和TransformUp（目标点前方）两类。

Angle Constraint Right：顺时针角度约束范围。

Angle Constraint Left：逆时针角度约束范围。

Use Fixed Angle Constraint：勾选后，启用固定角度约束(0°~360°)。

2.PointConstraint

2.1属性及功能

Auto Update：自动更新，勾选后在每一帧调用当前点的约束求解，约束链上有Brain组件时自动禁用。

Cover Update When Small：优化选项，勾选后，如果是从Update调用且当前距离与约束距离非常接近，则不进行解算。适用于具有移动但没有旋转运动的节点。

Solver Iterations：求解器迭代次数，数值越高，模拟精度越高。

Self Weight：自身权重，当Constraints的Weight Type为Self Weight时，采用该权重进行当前点与目标点的约束修正。

Constraints：约束数据（PointConstraintData）列表。

Parents：父级约束点列表，如果没有配置则会在运行前自动获取。

2.2关键方法及功能

Awake()：
遍历当前点的所有约束目标，在目标点的parents列表中注册自身引用，为后续双向遍历与自动收集提供基础。

OnUpdate(bool fromUpdate = false)：
自动约束求解的入口，按solverIterations指定的次数循环调用SolveAllConstraints，多轮迭代约束链的位置。

SolveAllConstraints(bool fromUpdate = false)：
遍历constraints列表中的每条约束数据，依次调用SolveOneConstraint执行单条约束解算。

SolveOneConstraint(PointConstraintData, bool fromUpdate = false)：
核心算法。使用世界坐标计算当前点与目标点的初始向量（包含初始距离与初始方向）；根据距离约束确定期望距离（固定距离优先，否则在最小和最大距离之间）；若启用角度约束，则检查初始方向与参考方向的夹角，若超出约束范围则使用最大或最小约束角度作为最终角度；若启用固定角度约束则直接将该角度作为最终角度；使用长度为期望距离、方向为最终角度的向量减去初始距向量得出修正量；根据权重将修正量分配至自身和目标点；弹性系数控制每次解算回正的修正量比例。

SolveEntireChainConstraints(bool fromUpdate = false)：
从当前点出发，沿约束目标方向递归遍历整条链，逐点调用SolveAllConstraints，实现整链的全局约束求解。用于链式骨骼的末尾驱动或根部驱动场景。

ReachTargetWithoutEnd(Vector3)：
逆向运动学的另一种模式，利用现有的约束链和迭代机制实现逆向运动学，无需额外集成专用IK解算器库，保持了系统依赖的最小化。每轮均将当前点置于目标位置后再执行整链求解，适用于将当前点直接设置在目标位置的情况。

ReachTargetWithEnd(Vector3)：
对于一端固定的优化方法，在迭代次数的上半执行“将当前点置于目标位置”与“整链求解”，后半只执行整链求解。迭代后，整条约束链在满足所有关节限制的条件下自然逼近目标。

ApproachTarget(Vector3)：
将当前点的位置设置在目标位置后，进行多次迭代的整链求解，适用于当前点缓慢跟随目标位置的情况。

3.EventModule

3.1 关键方法及功能

AddEventListener(string, Action)：
注册无参事件监听。若事件名已存在，将回调追加到现有委托链；否则新建事件信息实例（EventInfo）并加入字典。

AddEventListener<TAction>(string, TAction)：
注册带参事件监听，通过泛型TAction支持任意单参委托类型（如Action<float>）。若事件名已存在，通过Delegate.Combine将回调合并；否则新建带参信息实例（MultiEventInfo<TAction>）。用于需携带数据的事件。

EventTrigger(string)：
触发无参事件。从字典中取出对应EventInfo并调用其action委托，所有注册的回调同步执行。

EventTrigger<T>(string, T)：
触发带参事件。从字典中取出对应MultiEventInfo<Action<T>>，以传入参数调用委托，所有注册的回调同步执行。

RemoveEventListener（）：
从指定事件的委托链中移除特定回调，支持无参和有参两种形式，用于行为退出时清理监听。

RemoveEvent(string)：
从字典中移除整个事件条目并销毁关联的委托链，用于彻底清除不再需要的事件。

Clear()：
遍历并销毁字典中所有事件信息，清空字典。供行为退出或切换时批量清理所有监听。

4.Behavior

生物行为类，负责定义生物的各种行为

4.1属性及功能

Behavior Name：行为名称。

Brain：所在的决策中心。

4.2关键方法及功能

Awake()：
初始化eventModule实例，并调用虚方法Init()。由于Init在Awake中执行，事件监听在Brain开始调度前就会完成注册。

Init()：
扩展接口，虚方法。子类在此注册事件监听、初始化行为专有变量、向Brain的状态字典写入初始值。此方法在Awake中被自动调用，无需手动触发。

OnEnter()：
生命周期回调。当Brain通过ChangeBehavior激活此行为时调用。子类可在此重置计时器、播放进入动画或设置初始状态。

OnUpdate()：
决策主循环。由Brain每帧调用一次。子类在此实现核心的行为逻辑，如读取Brain的状态字典、进行条件判断或状态转移等。

OnExit()：
生命周期回调。当Brain切换至其他行为时调用。子类可在此进行清理工作，如注销事件监听、保存状态、播放退出过渡效果。

5.Brain

生物实体的决策中枢，负责统一管理所有Receptor、Effector、PointConstraint和Behavior组件，注入中枢引用并按照固定顺序调度更新，同时维护可供所有组件共享的状态字典。

5.1属性及功能

Receptors：感受器列表，自动遍历约束链获取。

Effectors：效应器列表，自动遍历约束链获取。

PointConstraints：点约束列表，自动遍历约束链获取。

LineControllers：线渲染器列表，自动遍历约束链获取。

Behaviors：行为列表，在编辑器视图或程序中注册。

Cur Behavior：当前行为。

Inspector State：用于初始化状态字典，支持int、float、string、bool四种类型。

5.2关键方法及功能

Start()：
初始化入口。依次执行：将Inspector配置的初始状态写入字典；递归扫描所在约束链自动收集所有组件；确认当前行为并注入brain引用。

Update()：
调度核心。每帧按固定顺序驱动全部组件：先遍历所有Receptor调用OnUpdate()采集信息；再调用当前行为的OnUpdate()执行决策逻辑；然后遍历所有Effector调用OnUpdate()执行效果；之后遍历所有PointConstraint调用OnUpdate(true)驱动骨骼约束求解；最后调用所有LineController进行渲染。保证了“感知→决策→执行→更新姿态→视觉匹配”的正确流程。

ChangeBehavior()：
接受Behavior引用或行为名称，退出当前行为并进入目标行为。

状态字典读写方法组：
提供GetIntState、SetFloatState等类型安全的读写接口。Receptor通过写接口将感知结果写入，Behavior通过读接口获取信息用于决策，Effector亦可读取状态以调整执行细节。这些方法共同实现了组件间的“黑板”式数据共享。

获取组件方法组：
提供GetEffector、GetLineController等方法，通过字符串名称获取对应组件的引用。

AddEventListener/EventTrigger等方法组：
对当前行为的EventModule进行代理封装，外部调用方可通过Brain直接注册监听或触发事件，不用直接持有EventModule引用。

6.Receptor与Effector

分别作为感知层和执行层的抽象基类，定义了子类可选重写的扩展接口。二者几乎不包含实现逻辑，作用在于为Brain提供统一的类型来管理所有感知和执行组件。感受器子类可以重写OnUpdate来实现功能，效应器子类可重写OnUpdate和TriggerEffect来实现持续生效和瞬时触发的功能。

6.1属性及功能

Auto Update：自动更新，勾选后在每一帧调用当前点的约束求解，约束链上有Brain组件时自动禁用。

Name：感受器或效应器名称。

Brain：所在的决策中心。

7.LineController

将约束链的点约束数据转化为可视化的身体与物理碰撞体的关键组件。通过遍历点约束链，使用三次贝塞尔曲线插值生成平滑的身体线条，并可根据线条实时构建2D多边形碰撞体。

7.1属性及功能

Auto Update：自动更新，勾选后在每一帧调用当前点的约束求解，约束链上有Brain组件时自动禁用。

Controller Name：线控制器名称。

Brain：所在的决策中心。

LineRenderer：线渲染器的引用，可以自动获取同一物体上的线渲染器。

Insert Point Count：通过三次贝塞尔曲线向每两个约束点间插入点的数量。

Z Offset：线渲染器每个点的Z轴偏移，通过调整该值可以调节线条与其他物体的遮罩关系。

Is Target Direction：勾选后将沿点约束的首个约束目标方向遍历画线，否则沿父级列表的首个点约束方向遍历画线。

End：结束点，遍历到结束点时停止画线，否则一直遍历到约束链终点。

Apply Collider：启用碰撞，开启后实时生成多边形碰撞体。

7.2关键方法及功能

Start()：
初始化LineRenderer组件引用；若启用碰撞体则获取或创建PolygonCollider2D和Rigidbody2D组件；调用UpdateTransformList完成初始线条生成。

UpdateTransformList()：
遍历与插值生成核心。从自身所在节点的PointConstraint出发，根据isTargetDirection决定的方向遍历约束链。在每两个相邻约束点之间调用AddBezierSegment插入若干贝塞尔采样点，实现光滑曲线。遍历终止于指定end节点或链的尽头。所有采样点将存入LineRenderer的linePoints列表用于渲染。

AddBezierSegment(Transform, Transform, Transform)：
在两个相邻约束点之间构建三次贝塞尔曲线段。利用前一段的方向和后一段的方向分别计算控制点1和控制点2，使曲线在节点处切线连续。按insertPointCount指定的数量在段间均匀采样，通过GetBezierPoint计算每个采样点的世界坐标并加入linePoints列表。

GetBezierPoint(Vector3, Vector3, Vector3, Vector3, float)：
标准三次贝塞尔曲线公式的逐点计算。根据参数t（0~1）和四个控制点返回曲线上对应点的世界坐标，并附加zOffset偏移以避免与骨骼节点重叠导致的穿模。

Update()：
由Brain驱动的Effector.OnUpdate调用。每帧调用UpdateTransformList刷新线条采样点，将linePoints赋给LineRenderer显示，若启用碰撞体则调用UpdateCollider更新碰撞形状。

UpdateCollider()：
将linePoints转为本地坐标，沿路径方向计算每点的切线和半宽（宽度由LineRenderer的宽度曲线和widthMultiplier决定）；在中段生成左右轮廓点，在起点和终点各生成半圆；将起点半圆、左侧轮廓、终点半圆、右侧轮廓按顺序组装为闭合多边形路径，赋给PolygonCollider2D。此过程每帧执行，碰撞体实时追随身体形变。

GenerateSemiCircle(Vector2, Vector2, float, int, bool)：
在路径端点生成半圆轮廓点。根据切线方向确定半圆的起始角度和终止角度，按LineRenderer的numCapVertices数值生成弧线点集，使碰撞体端部圆滑而非尖角。
