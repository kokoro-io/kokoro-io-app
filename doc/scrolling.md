# Message Scrolling Guidline

## Features

### Loading Trigger

- [x] ビューポートが上端から`300px`以内にあり、最古のメッセージが取得されていない場合
- [x] ビューポートが下端から`300px`以内にあり、新着メッセージの通知を受信している場合
- [x] ビューポートが下端にあり、更に下へのスクロールを行おうとした場合
- [ ] 新着メッセージの表示をクリックした場合
- [ ] (Future) 特定のメッセージが指定された場合

### Scrolling on inserting messages

- [ ] (Future) 特定のメッセージが指定された場合
- [x] 新着メッセージの通知より読み込みを行った場合、取得した最古メッセージを表示しつつ下方へスクロールする
- [x] ビューポートが下端から`*px`以内にあり新着メッセージを挿入する場合、既存の最下メッセージを表示しつつ下方へスクロールする
- [x] 挿入するメッセージの直後にある既存メッセージの上端がビューポート上端より上にある場合、挿入した要素の高さだけ上方へスクロールする
- [x] その他の場合はスクロール制御を行わない

### Message resizing triggers

- [ ] window.onresize ビューポート内に最下メッセージが含まれる場合、メッセージのビューポート下端からの位置を維持する

Apply same rules as inserting.

- [x] img.onload
- [x] MessageUpdated (id collision)

### Performance optimization

- [x] Hide offscreen elements if the all `<img>`s have loaded.
