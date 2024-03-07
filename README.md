# Procedural-generation

專案內容：
1. 在一平面上生成不重疊、不超出平面範圍的物件。最簡單的方法是隨機選擇生成座標並在 AABB 重疊與超出範圍時重新選擇新座標，但是當平面上的物件密度越高，重疊導致需要重新生成的機率也會隨之增加。如果從一個紀錄適當位置的列表取得新作標，這樣將降低重疊的機率，比隨機投飛鏢式生成的效能更好，因此，將平面依單位面積想像為網格，使用 `NativeArray<float3x2>` 作為網格載體，網格數（Array Size）為平面總面積 （x * z），使每個格子可以儲存三組 (x, z) 座標（想像三個大小相同的圓形可以同時都覆蓋到一個較小的正方形，如果物件是不旋轉的正方形，則一個格子可以同時覆蓋四個大正方形）；將生成物件的 AABB 所占範圍最外圈的所有格子中紀錄 AABB center ， AABB 以內完全覆蓋的格子或是一個格子集滿的三組座標則從適當位置列表刪除；同時，生成物件後也使用 AABB 最外圈的格子中儲存的座標與AABB center進行兩個半徑長的距離檢測。

2. 在平面中間區域隨機生成角色與其視野物件。限制 y 軸位移與 x、 z 軸旋轉，利用碰撞物理使角色生成在靜態物件內部時會被推出。

3. 找出視野範圍內的所有物件。使用視野物件作為 Trigger，紀錄觸發的物件。

4. 找出角色在視野範圍內可以"看到"物件。使用角色的 AABB center 作為起點、其他物件的 AABB center 作為終點，對所有物件分別進行 Raycast ，繪製所有 Raycast 路徑，命中後修改路徑顏色，如果沒有命中，則在終點添加標記。

了解ECS架構：
![](/Scripts/Unity%20ECS%20Structure.png)
