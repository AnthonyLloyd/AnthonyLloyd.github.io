---
layout: default
title: Home
---

## Posts

{% assign posts = site.posts | where_exp: "title", "title != 'Data-First Architecture - Asset Management Case Study'" %}
{% for post in posts %}
  {{ post.date | date: "%d %b %Y" }}
  [ {{ post.title }} ]({{ post.url }})
{% endfor %}
