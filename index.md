---
layout: default
title: Home
---

## Posts

{% for post in site.posts %}{% if post.exclude != true %}
  {{ post.date | date: "%d %b %Y" }}  
  [ {{ post.title }} ]({{ post.url }})
{% endif %}{% endfor %}
